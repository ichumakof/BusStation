using System;
using System.Collections.Generic;

namespace DAL.Interfaces
{
    public interface ITripRepository
    {
        List<DAL.Trips> GetTripsByDateRange(DateTime startDate, DateTime endDate);
        List<DAL.Trips> GetTripsByScheduleAndDate(int scheduleId, DateTime date);
        bool TripExists(int scheduleId, DateTime departure);
        int CreateTrip(DAL.Trips trip);
        void CreateTripsBatch(List<DAL.Trips> trips);
        int DeleteTripsByDateRange(DateTime startDate, DateTime endDate);
    }
}