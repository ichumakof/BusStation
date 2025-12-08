using System;

namespace BLL.Models
{
    public class ScheduleDTO
    {
        public int ScheduleID { get; set; }
        public int RouteID { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public string DaysOfWeek { get; set; } // "1,2,3,4,5"
        public bool IsActive { get; set; }

        // Маршрут (для информации)
        public string RouteInfo { get; set; }

        // Метод для получения дней недели как списка
        public System.Collections.Generic.List<int> GetDaysOfWeekList()
        {
            var days = new System.Collections.Generic.List<int>();

            if (!string.IsNullOrEmpty(DaysOfWeek))
            {
                var parts = DaysOfWeek.Split(',');
                foreach (var part in parts)
                {
                    if (int.TryParse(part.Trim(), out int day))
                        days.Add(day);
                }
            }

            return days;
        }
    }
}