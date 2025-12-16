using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Models
{
    public class SellTicketsRequest
    {
        public int TripID { get; set; }
        public int Quantity { get; set; }
        public int SoldByUserID { get; set; }
        public string TypeOfPayment { get; set; } // "Наличные" / "Карта" / ...
    }
}