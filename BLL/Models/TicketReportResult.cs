using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Models
{
    public class TicketReportResult
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int TotalSold { get; set; }
        public int TotalReturned { get; set; }
        public double TotalEarned { get; set; } // сумма только по Sold

        public List<TicketReportItem> Items { get; set; } = new List<TicketReportItem>();
    }
}