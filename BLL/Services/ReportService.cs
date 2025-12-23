// BLL/Services/ReportService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using BLL.Interfaces;
using BLL.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace BLL.Services
{
    public class ReportService : IReportService
    {
        public Task GeneratePdfReportAsync(TicketReportResult report, string outputPath)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            return Task.Run(() =>
            {
                var doc = new Document();
                var sec = doc.AddSection();

                var title = sec.AddParagraph("Отчет по продажам билетов");
                title.Format.Font.Size = 16;
                title.Format.SpaceAfter = Unit.FromCentimeter(0.5);

                sec.AddParagraph($"Период: {report.From:dd.MM.yyyy} — {report.To:dd.MM.yyyy}");
                sec.AddParagraph($"Дата формирования отчёта: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                sec.AddParagraph();

                var p = sec.AddParagraph();
                p.Format.Font.Size = 12;
                p.AddFormattedText("Общая сводка:", TextFormat.Bold);
                sec.AddParagraph($"Всего продано билетов: {report.TotalSold}");
                sec.AddParagraph($"Заработано: {report.TotalEarned:F2}");
                sec.AddParagraph();

                sec.AddParagraph("Детализация по маршрутам:");
                sec.AddParagraph();

                var table = sec.AddTable();
                table.Format.Font.Size = 11;
                table.Borders.Width = 0.5;
                table.AddColumn(Unit.FromCentimeter(8));
                table.AddColumn(Unit.FromCentimeter(3));
                table.AddColumn(Unit.FromCentimeter(3));
                table.AddColumn(Unit.FromCentimeter(4));

                var header = table.AddRow();
                header.Shading.Color = Colors.LightGray;
                header.Cells[0].AddParagraph("Маршрут");
                header.Cells[1].AddParagraph("Продано");
                header.Cells[3].AddParagraph("Заработано");

                if (report.Items != null)
                {
                    foreach (var item in report.Items)
                    {
                        var row = table.AddRow();
                        row.Cells[0].AddParagraph(item.RouteTitle);
                        row.Cells[1].AddParagraph(item.SoldCount.ToString());
                        row.Cells[3].AddParagraph(item.EarnedSum.ToString("F2"));
                    }
                }

                // Сохраняем
                var renderer = new PdfDocumentRenderer(true) { Document = doc };
                renderer.RenderDocument();

                // Создаём папку только если в outputPath есть путь
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                }

                // Сохраняем PDF — оборачиваем в try, чтобы вернуть полезный текст ошибки вызывающему коду
                try
                {
                    renderer.PdfDocument.Save(outputPath);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Не удалось сохранить PDF по пути '{outputPath}': {ex.Message}", ex);
                }
            });
        }
    }
}