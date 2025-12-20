using System.Threading.Tasks;
using BLL.Models;

namespace BLL.Interfaces
{
    public interface IReportService
    {
        // Генерирует PDF-файл по результату отчёта и сохраняет в outputPath (путь полный)
        Task GeneratePdfReportAsync(TicketReportResult report, string outputPath);
    }
}