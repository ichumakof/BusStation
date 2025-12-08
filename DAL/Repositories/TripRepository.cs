using DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAL.Repositories
{
    public class TripRepository : ITripRepository
    {
        private BusStationEntities _context;

        public TripRepository()
        {
            _context = new BusStationEntities();
        }

        public List<DAL.Trips> GetTripsByDateRange(DateTime startDate, DateTime endDate)
        {
            return _context.Trips
                .Where(t => t.DepartureDateTime >= startDate &&
                           t.DepartureDateTime <= endDate)
                .ToList();
        }

        public List<DAL.Trips> GetTripsByScheduleAndDate(int scheduleId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1).AddSeconds(-1);

            return _context.Trips
                .Where(t => t.ScheduleID == scheduleId &&
                           t.DepartureDateTime >= startOfDay &&
                           t.DepartureDateTime <= endOfDay)
                .ToList();
        }

        public bool TripExists(int scheduleId, DateTime departure)
        {
            return _context.Trips
                .Any(t => t.ScheduleID == scheduleId &&
                         t.DepartureDateTime == departure);
        }

        public int CreateTrip(DAL.Trips trip)
        {
            _context.Trips.Add(trip);
            _context.SaveChanges();
            return trip.TripID;
        }

        public void CreateTripsBatch(List<DAL.Trips> trips)
        {
            _context.Trips.AddRange(trips);
            _context.SaveChanges();
        }

        public int DeleteTripsByDateRange(DateTime startDate, DateTime endDate)
        {
            var tripsToDelete = _context.Trips
                .Where(t => t.DepartureDateTime >= startDate &&
                           t.DepartureDateTime <= endDate)
                .ToList();

            int count = tripsToDelete.Count;
            _context.Trips.RemoveRange(tripsToDelete);
            _context.SaveChanges();
            return count;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}