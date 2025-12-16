using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using BLL.Models;

namespace BLL.Services
{
    public class TicketPdfPrinter
    {
        // Сохраняет билеты в один PDF (каждый билет — новая страница) и открывает проводник,
        // Возвращает путь к файлу.
        public string SaveTicketsToPdf(IEnumerable<TicketPrintDTO> tickets, string outputFolder)
        {
            if (tickets == null) throw new ArgumentNullException(nameof(tickets));
            if (string.IsNullOrWhiteSpace(outputFolder)) throw new ArgumentNullException(nameof(outputFolder));

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            var doc = new Document();
            var style = doc.Styles["Normal"];
            style.Font.Name = "Arial";
            style.Font.Size = 11;

            foreach (var t in tickets)
            {
                var section = doc.AddSection();
                section.PageSetup.TopMargin = Unit.FromCentimeter(2);
                section.PageSetup.BottomMargin = Unit.FromCentimeter(2);
                section.PageSetup.LeftMargin = Unit.FromCentimeter(2);
                section.PageSetup.RightMargin = Unit.FromCentimeter(2);

                // Заголовок
                var pTitle = section.AddParagraph("Билет на автобус");
                pTitle.Format.Font.Size = 14;
                pTitle.Format.Font.Bold = true;
                pTitle.Format.SpaceAfter = Unit.FromMillimeter(4);

                section.AddParagraph("ООО \"BusStation\"").Format.SpaceAfter = Unit.FromMillimeter(3);

                var ticketNumber = t.TicketId.ToString("D6");
                section.AddParagraph($"Номер билета: {ticketNumber}").Format.SpaceAfter = Unit.FromMillimeter(3);

                // Маршрут — жирным (Иваново - {город назначения})
                var pRoute = section.AddParagraph(t.RouteTitle);
                pRoute.Format.Font.Bold = true;
                pRoute.Format.Font.Size = 12;
                pRoute.Format.SpaceAfter = Unit.FromMillimeter(4);

                section.AddParagraph($"Отправление: {t.DepartureDateTime:dd.MM.yyyy} - {t.DepartureDateTime:HH:mm}").Format.SpaceAfter = Unit.FromMillimeter(3);
                section.AddParagraph($"Прибытие: {t.ArrivalDateTime:dd.MM.yyyy} - {t.ArrivalDateTime:HH:mm}").Format.SpaceAfter = Unit.FromMillimeter(3);

                section.AddParagraph($"Рейс № {t.TripNumber}").Format.SpaceAfter = Unit.FromMillimeter(4);

                // Номер места — жирным
                var pSeat = section.AddParagraph($"Место № {t.SeatNumber}");
                pSeat.Format.Font.Bold = true;
                pSeat.Format.Font.Size = 12;
                pSeat.Format.SpaceAfter = Unit.FromMillimeter(4);

                section.AddParagraph($"Оплата: {t.PaymentType}. Сумма: {t.Price:F2} руб.").Format.SpaceAfter = Unit.FromMillimeter(4);

                // Транспорт
                var pTransport = section.AddParagraph($"Транспорт: {t.BusModel}");
                pTransport.Format.Font.Size = 12;
                pTransport.Format.SpaceAfter = Unit.FromMillimeter(4);

                var footer = section.AddParagraph($"Сформировано: {t.PurchaseDateTime:dd.MM.yyyy HH:mm}. Кассир: {t.CashierName}");
                footer.Format.Font.Size = 9;
                footer.Format.Alignment = ParagraphAlignment.Left;
            }

            var renderer = new PdfDocumentRenderer(unicode: true) { Document = doc };
            renderer.RenderDocument();

            var fileName = $"Билеты_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var outputPath = Path.Combine(outputFolder, fileName);
            renderer.PdfDocument.Save(outputPath);

            // Открыть проводник и выделить файл
            try
            {
                Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
            }
            catch
            {
                try { Process.Start("explorer.exe", $"\"{outputFolder}\""); } catch { }
            }

            return outputPath;
        }
    }
}