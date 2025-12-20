using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Models
{
    public class TicketReportItem
    {
        public int RouteID { get; set; }           // идентификатор маршрута (0 если не определён)
        public string RouteTitle { get; set; }     // "Иваново - <город назначения>"
        public int SoldCount { get; set; }
        public int ReturnedCount { get; set; }
        public double EarnedSum { get; set; }      // сумма по проданным билетам (только Sold)
    }
}