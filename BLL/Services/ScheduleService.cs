using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BLL.Interfaces;
using BLL.Models;
using DAL.Interfaces;
using DAL.Repositories;

namespace BLL.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleRepository _repo;

        public ScheduleService() : this(new ScheduleRepository()) { }

        public ScheduleService(IScheduleRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<IList<RouteDTO>> GetRoutesAsync()
        {
            return await Task.Run(() =>
            {
                var routes = _repo.GetRoutes();
                var cities = _repo.GetCities();

                var cityDict = new Dictionary<int, string>();
                if (cities != null)
                {
                    foreach (var c in cities)
                    {
                        var idProp = c.GetType().GetProperty("CityID", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase)
                                     ?? c.GetType().GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                        var nameProp = c.GetType().GetProperty("CityName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase)
                                       ?? c.GetType().GetProperty("Name", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                        if (idProp == null || nameProp == null) continue;
                        var idObj = idProp.GetValue(c, null);
                        var nameObj = nameProp.GetValue(c, null) as string;
                        if (idObj == null || string.IsNullOrWhiteSpace(nameObj)) continue;
                        if (int.TryParse(idObj.ToString(), out int id) && !cityDict.ContainsKey(id))
                            cityDict[id] = nameObj;
                    }
                }

                const string originCity = "Иваново";
                var list = routes.Select(r =>
                {
                    string destName = null;
                    var arrivalProp = r.GetType().GetProperty("ArrivalPointId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase)
                                      ?? r.GetType().GetProperty("ArrivalPointID", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                    if (arrivalProp != null)
                    {
                        var val = arrivalProp.GetValue(r, null);
                        if (val != null && int.TryParse(val.ToString(), out int arrivalId) && cityDict.ContainsKey(arrivalId))
                            destName = cityDict[arrivalId];
                    }

                    var info = !string.IsNullOrWhiteSpace(destName)
                        ? $"{originCity} - {destName}"
                        : $"{originCity} - #{r.RouteID}";

                    return new RouteDTO { RouteID = r.RouteID, RouteInfo = info };
                }).ToList();

                return list as IList<RouteDTO>;
            });
        }

        public async Task<IList<SimpleItemDTO>> GetDriversAsync()
        {
            return await Task.Run(() =>
            {
                var drivers = _repo.GetDrivers();
                return drivers.Select(d => new SimpleItemDTO
                {
                    Id = GetIntId(d, new[] { "DriverID", "Id" }),
                    Title = BuildDriverTitle(d)
                }).OrderBy(x => x.Title).ToList() as IList<SimpleItemDTO>;
            });
        }

        public async Task<IList<SimpleItemDTO>> GetBusesAsync()
        {
            return await Task.Run(() =>
            {
                var buses = _repo.GetBuses();
                return buses.Select(b => new SimpleItemDTO
                {
                    Id = GetIntId(b, new[] { "BusID", "Id" }),
                    Title = BuildBusTitle(b)
                }).OrderBy(x => x.Title).ToList() as IList<SimpleItemDTO>;
            });
        }

        public async Task<int> GenerateScheduleAsync(ScheduleGenerationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.StartDate.Date > request.EndDate.Date) throw new ArgumentException("StartDate > EndDate");
            if (request.DaysOfWeek == null || request.DaysOfWeek.Count == 0)
                throw new ArgumentException("Не выбраны дни недели");

            var time = request.DepartureTimeOfDay.HasValue ? request.DepartureTimeOfDay.Value : TimeSpan.FromHours(8);

            return await Task.Run(() =>
            {
                var dates = EachDate(request.StartDate.Date, request.EndDate.Date)
                            .Where(d => request.DaysOfWeek.Contains(d.DayOfWeek))
                            .Select(d => d.Date + time)
                            .ToList();

                if (dates.Count == 0) return 0;

                return _repo.CreateTripsForRoute(request.RouteID, dates, request.SkipExisting, request.BusID, request.DriverID, request.Price);
            });
        }

        private IEnumerable<DateTime> EachDate(DateTime from, DateTime thru)
        {
            for (var day = from; day <= thru; day = day.AddDays(1))
                yield return day;
        }

        // Helpers (рефлексия)
        private static int? GetDestinationCityId(object route)
        {
            if (route == null) return null;
            var t = route.GetType();
            var propNames = new[] { "DestinationCityID", "ToCityID", "ArrivalCityID", "CityToID", "DestinationID", "ToID" };
            foreach (var name in propNames)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p == null) continue;
                var val = p.GetValue(route, null);
                if (val == null) continue;
                int id;
                if (int.TryParse(val.ToString(), out id)) return id;
            }
            return null;
        }

        private static PropertyInfo FindIdProperty(object obj, string[] candidates)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var name in candidates)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p != null) return p;
            }
            return null;
        }

        private static PropertyInfo FindStringProperty(object obj, string[] candidates)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var name in candidates)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(string)) return p;
            }
            return null;
        }

        private static int GetIntId(object obj, string[] candidates)
        {
            if (obj == null) return 0;
            var t = obj.GetType();
            foreach (var name in candidates)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p == null) continue;
                var v = p.GetValue(obj, null);
                int id;
                if (v != null && int.TryParse(v.ToString(), out id)) return id;
            }
            return 0;
        }

        private static string BuildDriverTitle(object driver)
        {
            if (driver == null) return "Водитель";
            var t = driver.GetType();

            var fullName = t.GetProperty("FullName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(driver, null) as string;
            if (!string.IsNullOrWhiteSpace(fullName)) return fullName;

            var last = t.GetProperty("LastName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(driver, null) as string;
            var first = t.GetProperty("FirstName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(driver, null) as string;
            var mid = t.GetProperty("MiddleName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(driver, null) as string;

            var parts = new[] { last, first, mid }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if (parts.Length > 0) return string.Join(" ", parts);

            var name = t.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(driver, null) as string;
            if (!string.IsNullOrWhiteSpace(name)) return name;

            var id = GetIntId(driver, new[] { "DriverID", "Id" });
            return $"Водитель #{id}";
        }

        private static string BuildBusTitle(object bus)
        {
            if (bus == null) return "Автобус";
            var t = bus.GetType();

            var plateProps = new[] { "PlateNumber", "RegNumber", "StateNumber", "Number", "RegistrationNumber" };
            string plate = null;
            foreach (var pName in plateProps)
            {
                var p = t.GetProperty(pName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p == null) continue;
                plate = p.GetValue(bus, null) as string;
                if (!string.IsNullOrWhiteSpace(plate)) break;
            }

            var model = t.GetProperty("Model", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(bus, null) as string
                        ?? t.GetProperty("Brand", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(bus, null) as string
                        ?? t.GetProperty("Make", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(bus, null) as string;

            if (!string.IsNullOrWhiteSpace(plate) && !string.IsNullOrWhiteSpace(model)) return $"{plate} ({model})";
            if (!string.IsNullOrWhiteSpace(plate)) return plate;
            if (!string.IsNullOrWhiteSpace(model)) return model;

            var id = GetIntId(bus, new[] { "BusID", "Id" });
            return $"Автобус #{id}";
        }
    }
}
