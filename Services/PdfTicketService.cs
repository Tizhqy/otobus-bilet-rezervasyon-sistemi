using OtobusBiletRezervasyon.DTOs.Ticket;
using OtobusBiletRezervasyon.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace OtobusBiletRezervasyon.Services
{
    /// <summary>
    /// QuestPDF + QRCoder kullanarak bilet PDF'i ve QR kodu üretir.
    /// Serverda dosya saklamaz, her seferinde bellekte üretir.
    /// </summary>
    public class PdfTicketService : IPdfTicketService
    {
        private readonly ILogger<PdfTicketService> _logger;

        public PdfTicketService(ILogger<PdfTicketService> logger)
        {
            _logger = logger;
        }

        public byte[] GenerateTicketPdf(TicketResponseDto ticket)
        {
            var qrBytes = GenerateQrCode(ticket);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                    // ── Header ──
                    page.Header().Element(header =>
                    {
                        header
                            .Background(Colors.Blue.Darken3)
                            .Padding(15)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("🚌 HamsiBus")
                                        .FontSize(22).Bold().FontColor(Colors.White);
                                    col.Item().Text("E-Bilet / E-Ticket")
                                        .FontSize(11).FontColor(Colors.Blue.Lighten4);
                                });

                                row.ConstantItem(80).AlignRight().AlignMiddle().Text(text =>
                                {
                                    text.Span($"#{ticket.Id}").FontSize(18).Bold().FontColor(Colors.Orange.Medium);
                                });
                            });
                    });

                    // ── Content ──
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Route info
                        col.Item().PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("KALKIS / DEPARTURE").FontSize(7).FontColor(Colors.Grey.Medium).Bold();
                                left.Item().Text(ticket.Departure.OriginCity).FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                                left.Item().Text(ticket.Departure.OriginStation).FontSize(8).FontColor(Colors.Grey.Darken1);
                                left.Item().PaddingTop(3).Text(ticket.Departure.DepartureTime.ToString("dd MMM yyyy")).FontSize(9);
                                left.Item().Text(ticket.Departure.DepartureTime.ToString("HH:mm")).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                            });

                            row.ConstantItem(40).AlignCenter().AlignMiddle()
                                .Text("→").FontSize(20).FontColor(Colors.Orange.Medium);

                            row.RelativeItem().Column(right =>
                            {
                                right.Item().Text("VARIS / ARRIVAL").FontSize(7).FontColor(Colors.Grey.Medium).Bold();
                                right.Item().Text(ticket.Departure.DestinationCity).FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                                right.Item().Text(ticket.Departure.DestinationStation).FontSize(8).FontColor(Colors.Grey.Darken1);
                                right.Item().PaddingTop(3).Text(ticket.Departure.ArrivalTime.ToString("dd MMM yyyy")).FontSize(9);
                                right.Item().Text(ticket.Departure.ArrivalTime.ToString("HH:mm")).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                            });
                        });

                        // Divider
                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Bus + Seat + Price row
                        col.Item().PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("OTOBUS / BUS").FontSize(7).FontColor(Colors.Grey.Medium).Bold();
                                var busLabel = string.IsNullOrWhiteSpace(ticket.Departure.BusType)
                                    ? ticket.Departure.BusPlateNumber
                                    : $"{ticket.Departure.BusType} — {ticket.Departure.BusPlateNumber}";
                                c.Item().Text(busLabel).FontSize(10).Bold();
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("KOLTUK / SEAT").FontSize(7).FontColor(Colors.Grey.Medium).Bold();
                                var seatText = ticket.Passengers.Any()
                                    ? string.Join(", ", ticket.Passengers.Select(p => p.SeatNumber))
                                    : "-";
                                c.Item().Text(seatText).FontSize(10).Bold();
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("TOPLAM / TOTAL").FontSize(7).FontColor(Colors.Grey.Medium).Bold();
                                c.Item().Text($"${ticket.TotalPrice:0.00}").FontSize(12).Bold().FontColor(Colors.Green.Darken2);
                            });
                        });

                        // Divider
                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Passengers table
                        if (ticket.Passengers.Any())
                        {
                            col.Item().Text("YOLCULAR / PASSENGERS").FontSize(7).FontColor(Colors.Grey.Medium).Bold();
                            col.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                // Table header
                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                        .Text("#").FontSize(8).Bold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                        .Text("Ad Soyad / Name").FontSize(8).Bold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(4)
                                        .Text("Koltuk / Seat").FontSize(8).Bold();
                                });

                                int idx = 1;
                                foreach (var p in ticket.Passengers)
                                {
                                    var bgColor = idx % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                                    table.Cell().Background(bgColor).Padding(4).Text($"{idx}").FontSize(8);
                                    table.Cell().Background(bgColor).Padding(4).Text($"{p.FirstName} {p.LastName}").FontSize(9);
                                    table.Cell().Background(bgColor).Padding(4).Text(p.SeatNumber).FontSize(9).Bold();
                                    idx++;
                                }
                            });
                        }

                        // Spacer
                        col.Item().PaddingVertical(8);

                        // Payment info
                        if (ticket.Payment != null)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("ODEME YONTEMI / PAYMENT").FontSize(7).FontColor(Colors.Grey.Medium).Bold();
                                    c.Item().Text(ticket.Payment.Method).FontSize(9);
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("DURUM / STATUS").FontSize(7).FontColor(Colors.Grey.Medium).Bold();
                                    c.Item().Text(ticket.Payment.Status).FontSize(9);
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("REFERANS / REFERENCE").FontSize(7).FontColor(Colors.Grey.Medium).Bold();
                                    c.Item().Text(ticket.Payment.TransactionId ?? "-").FontSize(9).Bold();
                                });
                            });
                        }

                        // Spacer
                        col.Item().PaddingVertical(8);

                        // QR Code section
                        col.Item().AlignCenter().Column(qrCol =>
                        {
                            qrCol.Item().Border(1).BorderColor(Colors.Grey.Lighten2)
                                .Padding(10).Background(Colors.White)
                                .AlignCenter().Width(120).Image(qrBytes);

                            qrCol.Item().PaddingTop(4).AlignCenter()
                                .Text("Araca binerken bu QR kodu okutunuz")
                                .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                        });
                    });

                    // ── Footer ──
                    page.Footer().AlignCenter().Padding(8).Column(col =>
                    {
                        col.Item().AlignCenter().Text(text =>
                        {
                            text.Span("Bu belge elektronik olarak üretilmistir. ")
                                .FontSize(7).FontColor(Colors.Grey.Medium);
                            text.Span($"Olusturma: {DateTime.Now:dd.MM.yyyy HH:mm}")
                                .FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                        col.Item().AlignCenter().Text($"© {DateTime.Now.Year} HamsiBus — Tüm hakları saklıdır.")
                            .FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateQrCode(TicketResponseDto ticket)
        {
            var qrData = BuildQrPayload(ticket);

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);

            return qrCode.GetGraphic(8);
        }

        public string GenerateQrCodeBase64DataUri(TicketResponseDto ticket)
        {
            var pngBytes = GenerateQrCode(ticket);
            var base64 = Convert.ToBase64String(pngBytes);
            return $"data:image/png;base64,{base64}";
        }

        /// <summary>
        /// QR koduna gömülecek veri: Bilet ID, referans numarası, güzergah ve tarih.
        /// </summary>
        private static string BuildQrPayload(TicketResponseDto ticket)
        {
            var reference = ticket.Payment?.TransactionId ?? "N/A";
            var seats = ticket.Passengers.Any()
                ? string.Join(",", ticket.Passengers.Select(p => p.SeatNumber))
                : "-";

            return string.Join("|", new[]
            {
                $"HamsiBus-Ticket",
                $"ID:{ticket.Id}",
                $"REF:{reference}",
                $"ROUTE:{ticket.Departure.OriginCity}-{ticket.Departure.DestinationCity}",
                $"DATE:{ticket.Departure.DepartureTime:yyyyMMdd-HHmm}",
                $"SEATS:{seats}",
                $"PRICE:{ticket.TotalPrice:0.00}"
            });
        }
    }
}
