using System;

namespace BLL.Models
{
    public class TripDTO
    {
        public int TripID { get; set; }
        public int RouteID { get; set; }
        public int ScheduleID { get; set; }
        public int BusID { get; set; }
        public int DriverID { get; set; }
        public DateTime DepartureDateTime { get; set; }
        public DateTime ArrivalDateTime { get; set; }
        public double Price { get; set; }
        public int AvailableSeats { get; set; }
        public string Status { get; set; }

        // Для отображения
        public string ArrivalName { get; set; }
        public string RouteName { get; set; }
        public string BusNumber { get; set; }
        public string DriverName { get; set; }
        public string FormattedDeparture => DepartureDateTime.ToString("dd.MM.yyyy HH:mm");
        public string FormattedArrival => ArrivalDateTime.ToString("dd.MM.yyyy HH:mm");
    }
}