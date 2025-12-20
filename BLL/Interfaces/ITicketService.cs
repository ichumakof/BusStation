using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Models;

namespace BLL.Interfaces
{
    public interface ITicketService
    {
        // Актуальное кол-во свободных мест из базы
        Task<int> GetAvailableSeatsAsync(int tripId);

        // Продаёт билеты и возвращает список созданных TicketID
        Task<List<int>> SellTicketsAsync(SellTicketsRequest request);

        // Подготавливает данные для печати по списку id билетов
        Task<List<TicketPrintDTO>> GetTicketsForPrintingAsync(IEnumerable<int> ticketIds);

        // Новый метод: формирование агрегированных данных для отчёта.
        Task<List<CityDTO>> GetAllCitiesAsync();
        Task<TicketReportResult> GetTicketsReportAsync(DateTime from, DateTime to, IList<int> cityIds = null);
    }
}
