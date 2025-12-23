using System;
using System.Linq;
using System.Threading.Tasks;
using BLL.Interfaces;
using DAL;

namespace BLL.Services
{
    public class RouteManagerService : IRouteManagerService
    {
        public RouteManagerService()
        {
        }

        public async Task<bool> CityExistsAsync(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName)) return false;

            var normalized = cityName.Trim().ToLower();

            using (var db = new BusStationEntities())
            {
                return await Task.Run(() =>
                    db.Cities.Any(c => c.CityName.ToLower() == normalized)
                );
            }
        }

        public async Task<int> CreateCityAsync(string cityName, bool region)
        {
            if (string.IsNullOrWhiteSpace(cityName)) throw new ArgumentException("cityName");

            var name = cityName.Trim();

            using (var db = new BusStationEntities())
            {
                var city = new Cities
                {
                    CityName = name,
                    Region = region
                };
                db.Cities.Add(city);
                await Task.Run(() => db.SaveChanges());
                return city.CityID;
            }
        }

        public async Task<int> CreateRouteAsync(int arrivalPointId, int distanceKm, int durationMinutes)
        {
            using (var db = new BusStationEntities())
            {
                var route = new Routes
                {
                    ArrivalPointID = arrivalPointId,
                    Distance = distanceKm,
                    DurationMinutes = durationMinutes
                };
                db.Routes.Add(route);
                await Task.Run(() => db.SaveChanges());
                return route.RouteID;
            }
        }

        public async Task<(bool Success, int RouteId, string ErrorMessage)> CreateRouteWithCityAsync(string cityName, int distanceKm, int durationMinutes)
        {
            if (string.IsNullOrWhiteSpace(cityName))
                return (false, 0, "Имя города не может быть пустым.");

            try
            {
                var normalized = cityName.Trim();

                using (var db = new BusStationEntities())
                {
                    using (var tx = db.Database.BeginTransaction())
                    {
                        var exists = db.Cities.Any(c => c.CityName.ToLower() == normalized.ToLower());
                        if (exists)
                        {
                            return (false, 0, "Город с таким названием уже существует.");
                        }

                        var city = new Cities
                        {
                            CityName = normalized,
                            Region = true
                        };
                        db.Cities.Add(city);
                        db.SaveChanges();

                        var route = new Routes
                        {
                            ArrivalPointID = city.CityID,
                            Distance = distanceKm,
                            DurationMinutes = durationMinutes
                        };
                        db.Routes.Add(route);
                        db.SaveChanges();

                        tx.Commit();

                        return (true, route.RouteID, null);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }
    }
}
