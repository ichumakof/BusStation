using System;
using System.Collections.Generic;

namespace BLL.Models
{
    public class ScheduleGenerationRequest
    {
        public int RouteID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IList<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();

        // Необязательное время отправления в день. Если null — применим дефолт (08:00)
        public TimeSpan? DepartureTimeOfDay { get; set; }

        // Пропускать уже существующие рейсы в эти дни
        public bool SkipExisting { get; set; } = true;

        // Новое: выбранные в форме ресурсы
        public int? BusID { get; set; }
        public int? DriverID { get; set; }
        public double? Price { get; set; }
    }
}
