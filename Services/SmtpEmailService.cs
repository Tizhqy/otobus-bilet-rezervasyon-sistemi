using System.Net;
using System.Net.Mail;
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

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var host = _configuration["Smtp:Host"];
            var fromEmail = _configuration["Smtp:FromEmail"];
            var fromName = _configuration["Smtp:FromName"] ?? "HamsiBus";
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var port = _configuration.GetValue<int?>("Smtp:Port") ?? 587;
            var enableSsl = _configuration.GetValue<bool?>("Smtp:EnableSsl") ?? true;

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(fromEmail) ||
                string.IsNullOrWhiteSpace(toEmail) ||
                string.IsNullOrWhiteSpace(resetLink))
            {
                _logger.LogWarning("SMTP ayarlari eksik oldugu icin sifre sifirlama e-postasi gonderilemedi.");
                return false;
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = "HamsiBus Sifre Sifirlama",
                    Body = BuildPasswordResetBody(resetLink),
                    IsBodyHtml = false
                };
                message.To.Add(toEmail);

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrWhiteSpace(username))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sifre sifirlama e-postasi gonderilemedi. To={Email}", toEmail);
                return false;
            }
        }

        private static string BuildPasswordResetBody(string resetLink)
        {
            return
$@"Merhaba,

Sifrenizi sifirlamak icin asagidaki baglantiyi kullanin:
{resetLink}

Bu baglanti 1 saat gecerlidir.
Bu islemi siz yapmadiysaniz bu e-postayi dikkate almayin.

HamsiBus";
        }
    }
}
