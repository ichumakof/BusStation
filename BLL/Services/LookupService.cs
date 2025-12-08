using BLL.Interfaces;
using BLL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL;
using System.Data.Entity;

namespace BLL.Services
{
    public class LookupService : ILookupService
    {
        public async Task<IList<CityDTO>> GetCitiesAsync()
        {
            using (var ctx = new BusStationEntities())
            {
                var list = await ctx.Cities
                    .AsNoTracking()
                    .Select(c => new CityDTO { CityID = c.CityID, CityName = c.CityName })
                    .OrderBy(c => c.CityName)
                    .ToListAsync();

                return list;
            }
        }

        public async Task<IList<TripDTO>> GetTripsByArrivalCityAndDateAsync(int arrivalCityId, DateTime date)
        {
            using (var ctx = new BusStationEntities())
            {
                var targetDate = date.Date;

                var q = from t in ctx.Trips
                        join r in ctx.Routes on t.RouteID equals r.RouteID
                        join c in ctx.Cities on r.ArrivalPointID equals c.CityID
                        where r.ArrivalPointID == arrivalCityId
                              && DbFunctions.TruncateTime(t.DepartureDateTime) == targetDate
                        select new TripDTO
                        {
                            TripID = t.TripID,
                            RouteID = r.RouteID,
                            ArrivalName = c.CityName,
                            DepartureDateTime = t.DepartureDateTime,
                            Price = t.Price
                        };

                var list = await q.AsNoTracking().ToListAsync();

                if (targetDate == DateTime.Today)
                    list = list.Where(x => x.DepartureDateTime >= DateTime.Now).OrderBy(x => x.DepartureDateTime).ToList();
                else
                    list = list.OrderBy(x => x.DepartureDateTime).ToList();

                return list;
            }
        }
    }
}