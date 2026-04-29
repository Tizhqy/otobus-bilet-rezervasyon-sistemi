using OtobusBiletRezervasyon.DTOs.Ticket;
using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Services.FlowModels;
using OtobusBiletRezervasyon.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OtobusBiletRezervasyon.Services
{
    public class OdemeFlowService : IOdemeFlowService
    {
        private readonly ITicketService _ticketService;
        private readonly IPaymentService _paymentService;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICouponService _couponService;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly ILogger<OdemeFlowService> _logger;

        public OdemeFlowService(
            ITicketService ticketService,
            IPaymentService paymentService,
            ILogService logService,
            IHttpContextAccessor httpContextAccessor,
            ICouponService couponService,
            IAuthService authService,
            IEmailService emailService,
            ILogger<OdemeFlowService> logger)
        {
            _ticketService = ticketService;
            _paymentService = paymentService;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
            _couponService = couponService;
            _authService = authService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ServiceResult<OdemeSayfasiViewModel>> HazirlaOdemeSayfasiAsync(int biletId, int userId)
        {
            if (biletId <= 0)
                return ServiceResult<OdemeSayfasiViewModel>.Fail(ServiceResultType.ValidationError, "Gecersiz bilet.");

            var ticket = await _ticketService.GetTicketByIdAsync(biletId);
            if (ticket == null)
                return ServiceResult<OdemeSayfasiViewModel>.Fail(ServiceResultType.NotFound, "Bilet bulunamadi.");

            if (ticket.UserId != userId)
                return ServiceResult<OdemeSayfasiViewModel>.Fail(ServiceResultType.Forbidden, "Bu bilete erisim yetkiniz yok.");

            if (!IsPendingStatus(ticket.Status))
            {
                return ServiceResult<OdemeSayfasiViewModel>.Fail(
                    ServiceResultType.Conflict,
                    $"Bu bilet icin odeme yapilamaz. Durum: {ticket.Status}");
            }

            var timeRemaining = ticket.CreatedAt.AddMinutes(AppConfig.PaymentTimeoutMinutes) - DateTime.UtcNow;
            if (timeRemaining.TotalSeconds <= 0)
            {
                await _ticketService.CancelTicketAsync(biletId, userId);
                await _logService.LogAsync(userId, "ODEME_ZAMAN_ASIMI",
                    $"Bilet #{biletId} odeme suresi doldu, iptal edildi.", GetClientIpAddress());

                return ServiceResult<OdemeSayfasiViewModel>.Fail(
                    ServiceResultType.Expired,
                    "Odeme suresi doldu. Lutfen tekrar bilet alin.");
            }

            return ServiceResult<OdemeSayfasiViewModel>.Ok(new OdemeSayfasiViewModel
            {
                Ticket = ticket,
                KalanSaniye = (int)timeRemaining.TotalSeconds
            });
        }

        public async Task<ServiceResult<OdemeTamamlamaViewModel>> OdemeyiTamamlaAsync(
            int biletId,
            int userId,
            string odemeYontemi,
            string paymentToken,
            string idempotencyKey,
            string? cardLast4,
            string? couponCode)
        {
            var ticket = await _ticketService.GetTicketByIdAsync(biletId);
            if (ticket == null)
                return ServiceResult<OdemeTamamlamaViewModel>.Fail(ServiceResultType.NotFound, "Bilet bulunamadi.");

            if (ticket.UserId != userId)
                return ServiceResult<OdemeTamamlamaViewModel>.Fail(ServiceResultType.Forbidden, "Bu bilete erisim yetkiniz yok.");

            var originalTicketPrice = ticket.TotalPrice;

            if (!IsValidPaymentToken(paymentToken))
                return ServiceResult<OdemeTamamlamaViewModel>.Fail(ServiceResultType.ValidationError, "Odeme token'i gecersiz.");

            if (!IsValidIdempotencyKey(idempotencyKey))
                return ServiceResult<OdemeTamamlamaViewModel>.Fail(ServiceResultType.ValidationError, "Idempotency anahtari gecersiz.");

            if (!string.IsNullOrWhiteSpace(cardLast4) && !IsValidCardLast4(cardLast4))
                return ServiceResult<OdemeTamamlamaViewModel>.Fail(ServiceResultType.ValidationError, "Kart son 4 hanesi gecersiz.");

            if (!TryParsePaymentMethod(odemeYontemi, out var paymentMethod))
                return ServiceResult<OdemeTamamlamaViewModel>.Fail(ServiceResultType.ValidationError, "Gecersiz odeme yontemi.");

            var referenceNo = _paymentService.GenerateReferenceNumber(biletId, idempotencyKey);
            var couponCodeNormalized = couponCode?.Trim();

            if (IsConfirmedStatus(ticket.Status)
                && IsCompletedPaymentStatus(ticket.Payment?.Status)
                && string.Equals(ticket.Payment?.TransactionId, referenceNo, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(couponCodeNormalized))
                {
                    await _couponService.MarkCouponAsUsedAsync(userId, couponCodeNormalized);
                }

                return ServiceResult<OdemeTamamlamaViewModel>.Ok(new OdemeTamamlamaViewModel
                {
                    ReferenceNo = referenceNo,
                    OdemeYontemi = odemeYontemi
                });
            }

            if (!IsPendingStatus(ticket.Status))
            {
                return ServiceResult<OdemeTamamlamaViewModel>.Fail(
                    ServiceResultType.Conflict,
                    $"Gecersiz islem. Bilet durumu: {ticket.Status}");
            }

            if (_paymentService.IsPaymentExpired(ticket.CreatedAt, AppConfig.PaymentTimeoutMinutes))
            {
                await _ticketService.CancelTicketAsync(biletId, userId);
                await _logService.LogAsync(userId, "ODEME_ZAMAN_ASIMI",
                    $"Bilet #{biletId} odeme suresi doldu, iptal edildi.", GetClientIpAddress());

                return ServiceResult<OdemeTamamlamaViewModel>.Fail(ServiceResultType.Expired, "Odeme suresi doldu.");
            }

            var couponApplied = false;

            // Apply coupon logic if present
            if (!string.IsNullOrWhiteSpace(couponCodeNormalized))
            {
                var couponResult = await UygulaKuponAsync(biletId, userId, couponCodeNormalized);
                if (!couponResult.Success)
                    return ServiceResult<OdemeTamamlamaViewModel>.Fail(couponResult.Type, couponResult.Message ?? "Kupon hatasi.");

                var updatePriceResult = await _ticketService.UpdateTicketAndPaymentPriceAsync(biletId, couponResult.Data);
                if (!updatePriceResult)
                    return ServiceResult<OdemeTamamlamaViewModel>.Fail(ServiceResultType.Conflict, "Fiyat guncellenemedi.");

                couponApplied = true;
            }

            var completed = await _ticketService.CompletePaymentAsync(biletId, paymentMethod, referenceNo);
            if (!completed)
            {
                var latestTicket = await _ticketService.GetTicketByIdAsync(biletId);
                if (latestTicket != null
                    && latestTicket.UserId == userId
                    && IsConfirmedStatus(latestTicket.Status)
                    && IsCompletedPaymentStatus(latestTicket.Payment?.Status)
                    && string.Equals(latestTicket.Payment?.TransactionId, referenceNo, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(couponCodeNormalized))
                    {
                        await _couponService.MarkCouponAsUsedAsync(userId, couponCodeNormalized);
                    }

                    return ServiceResult<OdemeTamamlamaViewModel>.Ok(new OdemeTamamlamaViewModel
                    {
                        ReferenceNo = referenceNo,
                        OdemeYontemi = odemeYontemi
                    });
                }

                if (couponApplied)
                {
                    await _ticketService.UpdateTicketAndPaymentPriceAsync(biletId, originalTicketPrice);
                }

                return ServiceResult<OdemeTamamlamaViewModel>.Fail(
                    ServiceResultType.Conflict,
                    $"Gecersiz islem. Bilet durumu: {ticket.Status}");
            }

            if (couponApplied && !string.IsNullOrWhiteSpace(couponCodeNormalized))
            {
                await _couponService.MarkCouponAsUsedAsync(userId, couponCodeNormalized);
            }

            await _logService.LogAsync(userId, "ODEME_TAMAMLA",
                $"Bilet #{biletId} odendi. Referans: {referenceNo}, Yontem: {odemeYontemi}, Kart: {MaskLast4(cardLast4)}",
                GetClientIpAddress());

            var confirmedTicket = await _ticketService.GetTicketByIdAsync(biletId);
            if (confirmedTicket != null)
            {
                await SendTicketConfirmationEmailAsync(userId, confirmedTicket, referenceNo);
            }

            return ServiceResult<OdemeTamamlamaViewModel>.Ok(new OdemeTamamlamaViewModel
            {
                ReferenceNo = referenceNo,
                OdemeYontemi = odemeYontemi
            });
        }

        public async Task<(bool authorized, bool expired, int seconds)> KalanSureAsync(int biletId, int userId)
        {
            var ticket = await _ticketService.GetTicketByIdAsync(biletId);
            if (ticket == null)
                return (false, true, 0);

            if (ticket.UserId != userId)
                return (false, true, 0);

            var timeRemaining = ticket.CreatedAt.AddMinutes(AppConfig.PaymentTimeoutMinutes) - DateTime.UtcNow;
            if (timeRemaining.TotalSeconds <= 0)
                return (true, true, 0);

            return (true, false, (int)timeRemaining.TotalSeconds);
        }

        public async Task<ServiceResult<decimal>> UygulaKuponAsync(int biletId, int userId, string kuponKodu)
        {
            var ticket = await _ticketService.GetTicketByIdAsync(biletId);
            if (ticket == null || ticket.UserId != userId)
                return ServiceResult<decimal>.Fail(ServiceResultType.NotFound, "Bilet bulunamadı veya yetkisiz.");

            if (!IsPendingStatus(ticket.Status))
                return ServiceResult<decimal>.Fail(ServiceResultType.Conflict, "Bilet onaylanmış veya iptal edilmiş.");

            var coupon = await _couponService.GetValidCouponAsync(kuponKodu, userId);
            if (coupon == null)
                return ServiceResult<decimal>.Fail(ServiceResultType.ValidationError, "Geçersiz veya süresi dolmuş/kullanılmış kupon.");

            var newPrice = await _couponService.CalculateDiscountAsync(kuponKodu, ticket.TotalPrice, userId);
            return ServiceResult<decimal>.Ok(newPrice);
        }

        private static bool IsPendingStatus(string status)
        {
            return status.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                || status.Equals("BEKLEMEDE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsConfirmedStatus(string? status)
        {
            return !string.IsNullOrWhiteSpace(status)
                && (status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)
                    || status.Equals("ONAYLANDI", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsCompletedPaymentStatus(string? status)
        {
            return !string.IsNullOrWhiteSpace(status)
                && (status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                    || status.Equals("TAMAMLANDI", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsValidPaymentToken(string? paymentToken)
        {
            if (string.IsNullOrWhiteSpace(paymentToken) || paymentToken.Length != 64)
                return false;

            return paymentToken.All(c =>
                (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F'));
        }

        private static bool IsValidIdempotencyKey(string? idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return false;

            var key = idempotencyKey.Trim();
            if (key.Length < 16 || key.Length > 64)
                return false;

            return key.All(c => char.IsLetterOrDigit(c) || c == '-');
        }

        private static bool IsValidCardLast4(string? cardLast4)
        {
            return !string.IsNullOrWhiteSpace(cardLast4)
                && cardLast4.Length == 4
                && cardLast4.All(char.IsDigit);
        }

        private static string MaskLast4(string? cardLast4)
        {
            return IsValidCardLast4(cardLast4) ? $"****{cardLast4}" : "****";
        }

        private static bool TryParsePaymentMethod(string? method, out PaymentMethod paymentMethod)
        {
            paymentMethod = PaymentMethod.CreditCard;

            if (string.IsNullOrWhiteSpace(method))
                return false;

            var compact = method
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Trim()
                .ToLowerInvariant();

            return compact switch
            {
                "creditcard" => SetPaymentMethod(PaymentMethod.CreditCard, out paymentMethod),
                "debitcard" => SetPaymentMethod(PaymentMethod.DebitCard, out paymentMethod),
                "paypal" => SetPaymentMethod(PaymentMethod.Paypal, out paymentMethod),
                _ => false
            };
        }

        private static bool SetPaymentMethod(PaymentMethod value, out PaymentMethod paymentMethod)
        {
            paymentMethod = value;
            return true;
        }

        private string GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private async Task SendTicketConfirmationEmailAsync(int userId, TicketResponseDto ticket, string referenceNo)
        {
            var user = await _authService.GetCurrentUserAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            var mailSent = await _emailService.SendTicketConfirmationEmailAsync(
                user.Email,
                user.FirstName,
                ticket,
                referenceNo);

            if (!mailSent)
            {
                _logger.LogWarning("Bilet onay e-postasi gonderilemedi. UserId={UserId}, TicketId={TicketId}", userId, ticket.Id);
            }
        }
    }
}
