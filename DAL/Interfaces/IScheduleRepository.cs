using System;
using System.Collections.Generic;

namespace DAL.Interfaces
{
    public interface IScheduleRepository
    {
        // Список маршрутов для UI
        IList<Routes> GetRoutes();
        IList<Cities> GetCities();
        IList<Drivers> GetDrivers();
        IList<Buses> GetBuses();

        // Пакетное создание рейсов; возвращает количество созданных записей
        int CreateTripsForRoute(int routeId, IList<DateTime> departureDateTimes, bool skipExisting, int? busId, int? driverId, double? price);
    }
}
