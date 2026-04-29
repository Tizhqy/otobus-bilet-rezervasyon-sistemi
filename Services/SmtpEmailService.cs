using System.Net;
using System.Net.Mail;
using System.Text;
using OtobusBiletRezervasyon.DTOs.Ticket;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task<bool> SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink)
        {
            var subject = "HamsiBus | Sifre Sifirlama Talebi";
            var body = BuildPasswordResetBody(firstName, resetLink);
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName)
        {
            var subject = "HamsiBus | Hos Geldiniz";
            var body = BuildWelcomeBody(firstName);
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendTicketConfirmationEmailAsync(string toEmail, string firstName, TicketResponseDto ticket, string referenceNo)
        {
            var subject = $"HamsiBus | Biletiniz Hazir (#{ticket.Id})";
            var body = BuildTicketConfirmationBody(firstName, ticket, referenceNo);
            return SendEmailAsync(toEmail, subject, body);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtp = GetSmtpOptions();
            if (smtp == null || string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SMTP ayarlari eksik oldugu icin e-posta gonderilemedi. Subject={Subject}", subject);
                return false;
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(smtp.FromEmail, smtp.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                message.To.Add(toEmail);

                using var client = new SmtpClient(smtp.Host, smtp.Port)
                {
                    EnableSsl = smtp.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrWhiteSpace(smtp.Username))
                {
                    client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
                }

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta gonderilemedi. To={Email}, Subject={Subject}", toEmail, subject);
                return false;
            }
        }

        private SmtpOptions? GetSmtpOptions()
        {
            var host = _configuration["Smtp:Host"];
            var fromEmail = _configuration["Smtp:FromEmail"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
            {
                return null;
            }

            return new SmtpOptions
            {
                Host = host.Trim(),
                Port = _configuration.GetValue<int?>("Smtp:Port") ?? 587,
                EnableSsl = _configuration.GetValue<bool?>("Smtp:EnableSsl") ?? true,
                FromEmail = fromEmail.Trim(),
                FromName = (_configuration["Smtp:FromName"] ?? "HamsiBus").Trim(),
                Username = _configuration["Smtp:Username"],
                Password = _configuration["Smtp:Password"]
            };
        }

        private static string BuildPasswordResetBody(string firstName, string resetLink)
        {
            var safeName = WebUtility.HtmlEncode(GetDisplayName(firstName));
            var safeLink = WebUtility.HtmlEncode(resetLink);

            var content =
                $"""
                <p style="margin:0 0 14px;">Merhaba <strong>{safeName}</strong>,</p>
                <p style="margin:0 0 14px;">Sifre sifirlama talebiniz alindi. Asagidaki butonu kullanarak sifrenizi guvenli sekilde yenileyebilirsiniz.</p>
                <p style="margin:20px 0;">
                    <a href="{safeLink}" style="display:inline-block;background:#1a5276;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:700;">
                        Sifremi Sifirla
                    </a>
                </p>
                <p style="margin:0 0 8px;">Bu baglanti <strong>1 saat</strong> boyunca gecerlidir.</p>
                <p style="margin:0;">Bu islemi siz yapmadiysaniz bu e-postayi dikkate almayin ve hesabinizin guvenligi icin sifrenizi degistirin.</p>
                """;

            return BuildEmailLayout("Sifre Sifirlama", content);
        }

        private static string BuildWelcomeBody(string firstName)
        {
            var safeName = WebUtility.HtmlEncode(GetDisplayName(firstName));
            var content =
                $"""
                <p style="margin:0 0 14px;">Merhaba <strong>{safeName}</strong>,</p>
                <p style="margin:0 0 14px;">HamsiBus'a hos geldiniz. Hesabiniz basariyla olusturuldu.</p>
                <p style="margin:0 0 8px;">Sektor standartlarina uygun guvenli kullanim icin:</p>
                <ul style="margin:0 0 14px 20px;padding:0;">
                    <li style="margin:0 0 6px;">Sifrenizi kimseyle paylasmayin.</li>
                    <li style="margin:0 0 6px;">Toplu/agik aglarda oturumu acik birakmayin.</li>
                    <li style="margin:0;">Supheli durumda sifrenizi hemen yenileyin.</li>
                </ul>
                <p style="margin:0;">Iyi yolculuklar dileriz.</p>
                """;

            return BuildEmailLayout("Hesabiniz Hazir", content);
        }

        private static string BuildTicketConfirmationBody(string firstName, TicketResponseDto ticket, string referenceNo)
        {
            var safeName = WebUtility.HtmlEncode(GetDisplayName(firstName));
            var safeReference = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(referenceNo) ? "-" : referenceNo.Trim().ToUpperInvariant());
            var safeRoute = WebUtility.HtmlEncode($"{ticket.Departure.OriginCity} -> {ticket.Departure.DestinationCity}");
            var safeFrom = WebUtility.HtmlEncode($"{ticket.Departure.OriginStation}, {ticket.Departure.OriginCity}");
            var safeTo = WebUtility.HtmlEncode($"{ticket.Departure.DestinationStation}, {ticket.Departure.DestinationCity}");
            var safeDeparture = WebUtility.HtmlEncode(ticket.Departure.DepartureTime.ToString("dd.MM.yyyy HH:mm"));
            var safeArrival = WebUtility.HtmlEncode(ticket.Departure.ArrivalTime.ToString("dd.MM.yyyy HH:mm"));
            var safeBus = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(ticket.Departure.BusType)
                ? ticket.Departure.BusPlateNumber
                : $"{ticket.Departure.BusType} - {ticket.Departure.BusPlateNumber}");
            var safeSeatText = WebUtility.HtmlEncode(
                ticket.Passengers.Any()
                    ? string.Join(", ", ticket.Passengers.Select(p => p.SeatNumber))
                    : "-");
            var safeTotal = WebUtility.HtmlEncode($"${ticket.TotalPrice:0.00}");
            var safeTicketNo = WebUtility.HtmlEncode(ticket.Id.ToString());
            var safePaymentMethod = WebUtility.HtmlEncode(ticket.Payment?.Method ?? "-");

            var content =
                $"""
                <p style="margin:0 0 14px;">Merhaba <strong>{safeName}</strong>,</p>
                <p style="margin:0 0 14px;">Odemeniz basariyla tamamlandi. Bilet bilgileriniz asagidadir:</p>
                <table role="presentation" style="width:100%;border-collapse:collapse;font-size:14px;">
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Bilet No</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">#{safeTicketNo}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Referans</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeReference}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Guzergah</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeRoute}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Kalkis</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeFrom} - {safeDeparture}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Varis</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeTo} - {safeArrival}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Otobus</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeBus}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Koltuk</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeSeatText}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Odeme Yontemi</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safePaymentMethod}</td></tr>
                    <tr><td style="padding:8px;"><strong>Toplam Tutar</strong></td><td style="padding:8px;"><strong>{safeTotal}</strong></td></tr>
                </table>
                <p style="margin:14px 0 0;">Bu e-postayi ve referans numaranizi terminal girisinde hazir bulundurmaniz tavsiye edilir.</p>
                """;

            return BuildEmailLayout("Biletiniz Hazir", content);
        }

        private static string BuildEmailLayout(string title, string contentHtml)
        {
            var safeTitle = WebUtility.HtmlEncode(title);

            return
                $"""
                <!doctype html>
                <html>
                <head>
                  <meta charset="utf-8" />
                  <meta name="viewport" content="width=device-width, initial-scale=1" />
                  <title>{safeTitle}</title>
                </head>
                <body style="margin:0;padding:0;background:#f5f7fa;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
                  <div style="max-width:640px;margin:24px auto;padding:0 12px;">
                    <div style="background:#1a5276;color:#ffffff;padding:16px 20px;border-radius:12px 12px 0 0;">
                      <h1 style="margin:0;font-size:20px;">HamsiBus</h1>
                      <p style="margin:6px 0 0;font-size:13px;opacity:.9;">{safeTitle}</p>
                    </div>
                    <div style="background:#ffffff;padding:20px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 12px 12px;line-height:1.55;">
                      {contentHtml}
                    </div>
                    <p style="margin:12px 4px 0;font-size:12px;color:#6b7280;">
                      Bu e-posta otomatik olarak olusturulmustur. Destek icin lutfen HamsiBus ile iletisime gecin.
                    </p>
                  </div>
                </body>
                </html>
                """;
        }

        private static string GetDisplayName(string? firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                return "Yolcumuz";
            }

            return firstName.Trim();
        }

        private sealed class SmtpOptions
        {
            public string Host { get; init; } = string.Empty;
            public int Port { get; init; }
            public bool EnableSsl { get; init; }
            public string FromEmail { get; init; } = string.Empty;
            public string FromName { get; init; } = string.Empty;
            public string? Username { get; init; }
            public string? Password { get; init; }
        }
    }
}
