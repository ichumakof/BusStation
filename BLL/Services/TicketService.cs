using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using BLL.Interfaces;
using BLL.Models;
using DAL;

namespace BLL.Services
{
    public class TicketService : ITicketService
    {
        private BusStationEntities CreateContext() => new BusStationEntities();

        public async Task<int> GetAvailableSeatsAsync(int tripId)
        {
            using (var db = CreateContext())
            {
                var trip = await db.Trips
                                   .Include(t => t.Buses)
                                   .FirstOrDefaultAsync(t => t.TripID == tripId);

                if (trip == null) throw new InvalidOperationException("Рейс не найден.");

                var bus = trip.Buses;
                if (bus == null) throw new InvalidOperationException("Автобус не найден для рейса.");

                int capacity = bus.SeatsCount;

                int sold = await db.Tickets.CountAsync(x => x.TripID == tripId && x.Status != "Cancelled");

                int available = capacity - sold;
                return Math.Max(0, available);
            }
        }

        public async Task<List<int>> SellTicketsAsync(SellTicketsRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Quantity <= 0) throw new ArgumentException("Quantity must be > 0", nameof(request));
            if (string.IsNullOrWhiteSpace(request.TypeOfPayment)) throw new ArgumentException("TypeOfPayment required", nameof(request));

            var createdIds = new List<int>();

            using (var db = CreateContext())
            using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var trip = await db.Trips
                                       .Include(t => t.Routes)
                                       .Include(t => t.Buses)
                                       .FirstOrDefaultAsync(t => t.TripID == request.TripID);

                    if (trip == null) throw new InvalidOperationException("Рейс не найден.");

                    int destinationStopId = 0;
                    if (trip.Routes != null)
                    {
                        destinationStopId = trip.Routes.ArrivalPointID;
                    }
                    else
                    {
                        var route = await db.Routes.FirstOrDefaultAsync(r => r.RouteID == trip.RouteID);
                        if (route != null) destinationStopId = route.ArrivalPointID;
                    }

                    if (destinationStopId == 0)
                        throw new InvalidOperationException("Не удалось определить конечную остановку (ArrivalPointID) для данного маршрута.");

                    var occupiedSeatNumbers = await db.Tickets
                                                      .Where(t => t.TripID == request.TripID && t.Status != "Cancelled")
                                                      .Select(t => t.SeatNumber)
                                                      .ToListAsync();

                    int capacity = 0;
                    if (trip.Buses != null) capacity = trip.Buses.SeatsCount;

                    int tripAvailableSeats = trip?.AvailableSeats ?? 0;
                    if (capacity == 0 && tripAvailableSeats > 0) capacity = occupiedSeatNumbers.Count + tripAvailableSeats;

                    if (capacity == 0)
                    {
                        var tripSeatsProp = trip.GetType().GetProperties()
                                               .FirstOrDefault(p => p.Name.ToLower().Contains("seat") || p.Name.ToLower().Contains("capacity"));
                        if (tripSeatsProp != null)
                        {
                            var v = tripSeatsProp.GetValue(trip);
                            if (v != null && int.TryParse(v.ToString(), out int c)) capacity = c;
                        }
                    }

                    if (capacity <= 0)
                        throw new InvalidOperationException("Не удалось определить вместимость автобуса для рейса (capacity). Проверьте данные Buses/Trips.");

                    int free = capacity - occupiedSeatNumbers.Count;
                    if (free < request.Quantity)
                        throw new InvalidOperationException($"Недостаточно свободных мест. Свободно: {free}, требуется: {request.Quantity}");

                    var seatNumbersToUse = new List<int>(request.Quantity);
                    for (int num = 1; num <= capacity && seatNumbersToUse.Count < request.Quantity; num++)
                    {
                        if (!occupiedSeatNumbers.Contains(num))
                            seatNumbersToUse.Add(num);
                    }

                    if (seatNumbersToUse.Count < request.Quantity)
                        throw new InvalidOperationException("Не удалось подобрать номера мест для продажи.");

                    double price = 0;
                    try { price = trip.Price; } catch { price = 0; }

                    var now = DateTime.Now;
                    var ticketsToAdd = new List<DAL.Tickets>();

                    for (int i = 0; i < request.Quantity; i++)
                    {
                        var seatNum = seatNumbersToUse[i];

                        var ticket = new DAL.Tickets
                        {
                            TripID = request.TripID,
                            SoldByUserID = request.SoldByUserID,
                            PurchaseDateTime = now,
                            Price = price,
                            TypeOfPayment = request.TypeOfPayment,
                            Status = "Sold",
                            DestinationStopID = destinationStopId,
                            SeatNumber = seatNum
                        };

                        db.Tickets.Add(ticket);
                        ticketsToAdd.Add(ticket);
                    }

                    await db.SaveChangesAsync().ConfigureAwait(false);

                    createdIds.AddRange(ticketsToAdd.Select(t => t.TicketID));

                    var tripInDb = await db.Trips.FirstOrDefaultAsync(t => t.TripID == request.TripID);
                    if (tripInDb != null)
                    {
                        tripInDb.AvailableSeats = Math.Max(0, capacity - await db.Tickets.CountAsync(t => t.TripID == request.TripID && t.Status != "Cancelled"));
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }

                    tx.Commit();
                    return createdIds;
                }
                catch (DbUpdateException dbEx)
                {
                    tx.Rollback();
                    var inner = dbEx.InnerException?.InnerException?.Message ?? dbEx.InnerException?.Message ?? dbEx.Message;
                    throw new InvalidOperationException("Ошибка при сохранении билетов: " + inner, dbEx);
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Формирует DTO для печати по списку id билетов.
        /// Использует ArrivalPointID маршрута -> Cities.CityName для города назначения.
        /// </summary>
        public async Task<List<TicketPrintDTO>> GetTicketsForPrintingAsync(IEnumerable<int> ticketIds)
        {
            var ids = ticketIds?.ToList() ?? new List<int>();
            if (!ids.Any()) return new List<TicketPrintDTO>();

            using (var db = CreateContext())
            {
                var tickets = await db.Tickets
                                      .Where(t => ids.Contains(t.TicketID))
                                      .Include(t => t.Trips)
                                      .ToListAsync();

                var result = new List<TicketPrintDTO>(tickets.Count);
                foreach (var t in tickets)
                {
                    var trip = t.Trips;

                    // Определяем город назначения через Routes -> ArrivalPointID -> Cities.CityName
                    string destinationCityName = null;
                    try
                    {
                        int arrivalId = 0;
                        if (trip?.Routes != null)
                        {
                            arrivalId = trip.Routes.ArrivalPointID;
                        }
                        else if (trip != null)
                        {
                            var route = await db.Routes.FirstOrDefaultAsync(r => r.RouteID == trip.RouteID);
                            if (route != null) arrivalId = route.ArrivalPointID;
                        }

                        if (arrivalId != 0)
                        {
                            var city = await db.Cities.FirstOrDefaultAsync(c => c.CityID == arrivalId);
                            if (city != null) destinationCityName = city.CityName;
                        }
                    }
                    catch
                    {
                        destinationCityName = null;
                    }

                    string routeTitle = $"Иваново - {(destinationCityName ?? "город_назначения")}";

                    // Bus model
                    string busModel = "Неизвестно";
                    try
                    {
                        if (trip?.Buses != null) busModel = trip.Buses.Model ?? "Неизвестно";
                    }
                    catch { busModel = "Неизвестно"; }

                    // Cashier name
                    string cashierName = "Кассир";
                    try
                    {
                        var user = await db.Users.FirstOrDefaultAsync(u => u.UserID == t.SoldByUserID);
                        if (user != null)
                        {
                            var nameProp = user.GetType().GetProperties().FirstOrDefault(p => p.Name.ToLower().Contains("name") || p.Name.ToLower().Contains("fio") || p.Name.ToLower().Contains("full"));
                            var nameVal = nameProp?.GetValue(user) as string;
                            if (!string.IsNullOrEmpty(nameVal)) cashierName = nameVal;
                        }
                    }
                    catch { /* оставляем default */ }

                    int tripNumber = trip?.TripID ?? 0;

                    var dto = new TicketPrintDTO
                    {
                        TicketId = t.TicketID,
                        CashierName = cashierName,
                        PurchaseDateTime = t.PurchaseDateTime,
                        RouteTitle = routeTitle,
                        DepartureDateTime = trip?.DepartureDateTime ?? DateTime.MinValue,
                        ArrivalDateTime = trip?.ArrivalDateTime ?? DateTime.MinValue,
                        BusModel = busModel,
                        TripNumber = tripNumber,
                        SeatNumber = t.SeatNumber,
                        PaymentType = t.TypeOfPayment,
                        Price = t.Price
                    };

                    result.Add(dto);
                }

                return result;
            }
        }
        public async Task<List<CityDTO>> GetAllCitiesAsync()
        {
            using (var db = CreateContext())
            {
                return await db.Cities
                    .OrderBy(c => c.CityName)
                    .Select(c => new CityDTO { CityID = c.CityID, CityName = c.CityName })
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<TicketReportResult> GetTicketsReportAsync(DateTime from, DateTime to, IList<int> cityIds = null)
        {
            // Нормализуем границы: from at 00:00:00, to at 23:59:59.999...
            var fromDt = from.Date;
            var toDt = to.Date.AddDays(1).AddTicks(-1);

            using (var db = CreateContext())
            {
                // Собираем минимальный набор данных: Trip -> Route -> City
                var query = from t in db.Tickets
                            join tr in db.Trips on t.TripID equals tr.TripID into trj
                            from tr in trj.DefaultIfEmpty()
                            join r in db.Routes on tr.RouteID equals r.RouteID into rj
                            from r in rj.DefaultIfEmpty()
                            join c in db.Cities on r.ArrivalPointID equals c.CityID into cj
                            from c in cj.DefaultIfEmpty()
                            where t.PurchaseDateTime >= fromDt && t.PurchaseDateTime <= toDt
                            select new
                            {
                                Ticket = t,
                                Trip = tr,
                                Route = r,
                                City = c
                            };

                // Применяем фильтр по выбранным городам (если указан)
                if (cityIds != null && cityIds.Any())
                {
                    query = query.Where(x => x.City != null && cityIds.Contains(x.City.CityID));
                }

                var raw = await query
                            .Select(x => new
                            {
                                RouteID = (int?)(x.Route != null ? (int?)x.Route.RouteID : null),
                                CityID = (int?)(x.City != null ? (int?)x.City.CityID : null),
                                CityName = x.City != null ? x.City.CityName : null,
                                Status = x.Ticket.Status,
                                Price = (double?)x.Ticket.Price
                            })
                            .ToListAsync()
                            .ConfigureAwait(false);

                // Группировка по маршруту (RouteID). Если RouteID == null, положим 0.
                var groups = raw
                    .GroupBy(r => r.RouteID ?? 0)
                    .Select(g =>
                    {
                        var any = g.FirstOrDefault();
                        var cityName = any?.CityName ?? "неизвестно";
                        return new TicketReportItem
                        {
                            RouteID = g.Key,
                            RouteTitle = $"Иваново - {cityName}",
                            SoldCount = g.Count(x => x.Status == "Sold"),
                            ReturnedCount = g.Count(x => x.Status == "Returned"),
                            EarnedSum = g.Where(x => x.Status == "Sold").Sum(x => x.Price ?? 0)
                        };
                    })
                    .OrderByDescending(i => i.SoldCount)
                    .ToList();

                var result = new TicketReportResult
                {
                    From = fromDt,
                    To = toDt,
                    Items = groups,
                    TotalSold = groups.Sum(i => i.SoldCount),
                    TotalReturned = groups.Sum(i => i.ReturnedCount),
                    TotalEarned = groups.Sum(i => i.EarnedSum)
                };

                return result;
            }
        }
    }
}