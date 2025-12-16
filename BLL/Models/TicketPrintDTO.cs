using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Models
{
    public class TicketPrintDTO
    {
        public int TicketId { get; set; }              // id билета в БД
        public string CashierName { get; set; }        // имя кассира
        public DateTime PurchaseDateTime { get; set; } // время продажи
        public string RouteTitle { get; set; }         // "Иваново - город_назначения"
        public DateTime DepartureDateTime { get; set; }
        public DateTime ArrivalDateTime { get; set; }
        public string BusModel { get; set; }           // модель автобуса
        public int TripNumber { get; set; }            // если нужен номер рейса
        public int SeatNumber { get; set; }            // место №
        public string PaymentType { get; set; }        // вид оплаты
        public double Price { get; set; }              // сумма оплаты
    }
}
