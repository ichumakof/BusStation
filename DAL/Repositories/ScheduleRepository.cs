using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DAL.Interfaces;

namespace DAL.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        public IList<Routes> GetRoutes()
        {
            using (var ctx = new BusStationEntities())
            {
                return ctx.Routes.AsNoTracking().ToList();
            }
        }

        public IList<Cities> GetCities()
        {
            using (var ctx = new BusStationEntities())
            {
                return ctx.Cities.AsNoTracking().ToList();
            }
        }

        public IList<Drivers> GetDrivers()
        {
            using (var ctx = new BusStationEntities())
            {
                return ctx.Drivers.AsNoTracking().ToList();
            }
        }

        public IList<Buses> GetBuses()
        {
            using (var ctx = new BusStationEntities())
            {
                return ctx.Buses.AsNoTracking().ToList();
            }
        }

        public int CreateTripsForRoute(int routeId, IList<DateTime> departureDateTimes, bool skipExisting, int? busId, int? driverId, double? price)
        {
            if (departureDateTimes == null || departureDateTimes.Count == 0) return 0;

            using (var ctx = new BusStationEntities())
            {
                var route = ctx.Routes.AsNoTracking().FirstOrDefault(r => r.RouteID == routeId);
                if (route == null) throw new InvalidOperationException($"Маршрут {routeId} не найден");

                int durationMin = route.DurationMinutes.HasValue ? route.DurationMinutes.Value : 0;

                int resolvedBusId = busId.HasValue ? busId.Value :
                    ctx.Buses.AsNoTracking().OrderBy(b => b.BusID).Select(b => b.BusID).FirstOrDefault();
                if (resolvedBusId == 0) throw new InvalidOperationException("В базе отсутствуют автобусы (Buses). Невозможно создать рейсы.");

                int resolvedDriverId = driverId.HasValue ? driverId.Value :
                    ctx.Drivers.AsNoTracking().OrderBy(d => d.DriverID).Select(d => d.DriverID).FirstOrDefault();
                if (resolvedDriverId == 0) throw new InvalidOperationException("В базе отсутствуют водители (Drivers). Невозможно создать рейсы.");

                // Получаем вместимость выбранного автобуса
                var resolvedBusSeats = ctx.Buses.AsNoTracking()
                    .Where(b => b.BusID == resolvedBusId)
                    .Select(b => b.SeatsCount)
                    .FirstOrDefault();

                var minDate = departureDateTimes.Min().Date;
                var maxDate = departureDateTimes.Max().Date;

                HashSet<DateTime> existingDateSet = new HashSet<DateTime>();
                if (skipExisting)
                {
                    var existingDates = ctx.Trips
                        .Where(t => t.RouteID == routeId &&
                                    DbFunctions.TruncateTime(t.DepartureDateTime) >= minDate &&
                                    DbFunctions.TruncateTime(t.DepartureDateTime) <= maxDate)
                        .Select(t => DbFunctions.TruncateTime(t.DepartureDateTime).Value)
                        .ToList();

                    existingDateSet = new HashSet<DateTime>(existingDates.Select(d => d.Date));
                }

                double resolvedPrice = price.HasValue ? price.Value : 0.0;

                var toCreate = departureDateTimes
                    .Where(dt => !skipExisting || !existingDateSet.Contains(dt.Date))
                    .Select(dt => new Trips
                    {
                        RouteID = routeId,
                        DepartureDateTime = dt,
                        ArrivalDateTime = dt.AddMinutes(durationMin),
                        Status = "Planned",
                        BusID = resolvedBusId,
                        DriverID = resolvedDriverId,
                        Price = resolvedPrice,
                        AvailableSeats = resolvedBusSeats,
                        ScheduleID = null
                    })
                    .ToList();

                if (toCreate.Count == 0) return 0;

                ctx.Trips.AddRange(toCreate);
                return ctx.SaveChanges();
            }
        }
    }
}
