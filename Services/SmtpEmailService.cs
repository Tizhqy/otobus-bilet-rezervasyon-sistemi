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
            var subject = "HamsiBus | Password Reset Request";
            var body = BuildPasswordResetBody(firstName, resetLink);
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName)
        {
            var subject = "HamsiBus | Welcome";
            var body = BuildWelcomeBody(firstName);
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendTicketConfirmationEmailAsync(string toEmail, string firstName, TicketResponseDto ticket, string referenceNo)
        {
            var subject = $"HamsiBus | Your Ticket is Ready (#{ticket.Id})";
            var body = BuildTicketConfirmationBody(firstName, ticket, referenceNo);
            return SendEmailAsync(toEmail, subject, body);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtp = GetSmtpOptions();
            if (smtp == null || string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SMTP configuration is missing. Cannot send email. Subject={Subject}", subject);
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
                _logger.LogError(ex, "Failed to send email. To={Email}, Subject={Subject}", toEmail, subject);
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
                <p style="margin:0 0 14px;">Hello <strong>{safeName}</strong>,</p>
                <p style="margin:0 0 14px;">We received a request to reset your password. You can securely set a new password by clicking the button below.</p>
                <p style="margin:20px 0;">
                    <a href="{safeLink}" style="display:inline-block;background:#1a5276;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:700;">
                        Reset My Password
                    </a>
                </p>
                <p style="margin:0 0 8px;">This link is valid for <strong>1 hour</strong>.</p>
                <p style="margin:0;">If you did not request this, please ignore this email and change your password for your account's safety.</p>
                """;

            return BuildEmailLayout("Password Reset", content);
        }

        private static string BuildWelcomeBody(string firstName)
        {
            var safeName = WebUtility.HtmlEncode(GetDisplayName(firstName));
            var content =
                $"""
                <p style="margin:0 0 14px;">Hello <strong>{safeName}</strong>,</p>
                <p style="margin:0 0 14px;">Welcome to HamsiBus. Your account has been created successfully.</p>
                <p style="margin:0 0 8px;">For secure usage according to industry standards:</p>
                <ul style="margin:0 0 14px 20px;padding:0;">
                    <li style="margin:0 0 6px;">Do not share your password with anyone.</li>
                    <li style="margin:0 0 6px;">Do not stay logged in on public/open networks.</li>
                    <li style="margin:0;">Renew your password immediately if you suspect any suspicious activity.</li>
                </ul>
                <p style="margin:0;">We wish you a pleasant journey.</p>
                """;

            return BuildEmailLayout("Your Account is Ready", content);
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
                <p style="margin:0 0 14px;">Hello <strong>{safeName}</strong>,</p>
                <p style="margin:0 0 14px;">Your payment has been successfully processed. Your ticket details are below:</p>
                <table role="presentation" style="width:100%;border-collapse:collapse;font-size:14px;">
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Ticket No</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">#{safeTicketNo}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Reference</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeReference}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Route</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeRoute}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Departure</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeFrom} - {safeDeparture}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Arrival</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeTo} - {safeArrival}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Bus</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeBus}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Seat(s)</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safeSeatText}</td></tr>
                    <tr><td style="padding:8px;border-bottom:1px solid #e5e7eb;"><strong>Payment Method</strong></td><td style="padding:8px;border-bottom:1px solid #e5e7eb;">{safePaymentMethod}</td></tr>
                    <tr><td style="padding:8px;"><strong>Total Amount</strong></td><td style="padding:8px;"><strong>{safeTotal}</strong></td></tr>
                </table>
                <div style="text-align:center; margin-top:20px; padding: 15px; border: 1px dashed #cbd5e1; border-radius: 8px; background-color: #f8fafc;">
                    <p style="margin:0 0 10px; font-size: 13px; color: #64748b;">Your E-Ticket QR Code (Please scan when boarding)</p>
                    <img src="https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=HamsiBus-Ticket-{safeTicketNo}-{safeReference}" alt="Ticket QR Code" style="border-radius: 8px;" />
                </div>
                <p style="margin:14px 0 0;">It is recommended to have this email and your reference number ready at the terminal entrance.</p>
                """;

            return BuildEmailLayout("Your Ticket is Ready", content);
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
                  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap" rel="stylesheet">
                </head>
                <body style="margin:0;padding:0;background-color:#f4f7f6;font-family:'Inter', Arial, sans-serif;color:#333333;-webkit-font-smoothing:antialiased;">
                  <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color:#f4f7f6;padding:40px 0;">
                    <tr>
                      <td align="center">
                        <table role="presentation" width="600" border="0" cellspacing="0" cellpadding="0" style="background-color:#ffffff;border-radius:16px;box-shadow:0 10px 25px rgba(0,0,0,0.05);overflow:hidden;">
                          <!-- Header -->
                          <tr>
                            <td style="background:linear-gradient(135deg, #1e3c72 0%, #2a5298 100%);padding:40px 30px;text-align:center;">
                              <h1 style="margin:0;font-size:28px;color:#ffffff;font-weight:700;letter-spacing:1px;">🚌 HamsiBus</h1>
                              <p style="margin:10px 0 0;font-size:15px;color:#e0e7ff;font-weight:400;opacity:0.9;">{safeTitle}</p>
                            </td>
                          </tr>
                          <!-- Body Content -->
                          <tr>
                            <td style="padding:40px 30px;line-height:1.6;font-size:16px;color:#4b5563;">
                              {contentHtml}
                            </td>
                          </tr>
                          <!-- Footer -->
                          <tr>
                            <td style="background-color:#f8fafc;padding:30px;text-align:center;border-top:1px solid #e2e8f0;">
                              <p style="margin:0;font-size:13px;color:#64748b;line-height:1.5;">
                                This email was generated automatically.<br>
                                For support, please contact the <a href="#" style="color:#2a5298;text-decoration:none;font-weight:600;">HamsiBus Support Center</a>.
                              </p>
                              <p style="margin:15px 0 0;font-size:12px;color:#94a3b8;">
                                &copy; {DateTime.Now.Year} HamsiBus. All rights reserved.
                              </p>
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;
        }

        private static string GetDisplayName(string? firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                return "Passenger";
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
