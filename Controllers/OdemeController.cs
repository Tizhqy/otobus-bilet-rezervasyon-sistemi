using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Controllers
{
    [Authorize]
    public class OdemeController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly ILogService _logService;
        private readonly IPaymentService _paymentService;
        private const int PaymentTimeoutMinutes = 15;

        public OdemeController(
            ITicketService ticketService,
            ILogService logService,
            IPaymentService paymentService)
        {
            _ticketService = ticketService;
            _logService = logService;
            _paymentService = paymentService;
        }

        #region Odeme (Payment Page)

        [HttpGet]
        public async Task<IActionResult> Odeme(int biletId)
        {
            if (biletId <= 0)
            {
                TempData["Hata"] = "Gecersiz bilet.";
                return RedirectToAction("Liste", "Bilet");
            }

            int userId = GetCurrentUserId();
            var ticket = await _ticketService.GetTicketForUserAsync(userId, biletId);

            if (ticket == null)
            {
                TempData["Hata"] = "Bilet bulunamadi.";
                return RedirectToAction("Liste", "Bilet");
            }

            if (!ticket.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
                !ticket.Status.Equals("BEKLEMEDE", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Hata"] = "Bu bilet icin odeme yapilamaz. Durum: " + ticket.Status;
                return RedirectToAction("Liste", "Bilet");
            }

            var timeRemaining = GetPaymentRemainingTime(ticket.CreatedAt);

            if (timeRemaining.TotalSeconds <= 0)
            {
                await _ticketService.CancelTicketAsync(biletId, userId);
                await _logService.LogAsync(userId, "ODEME_ZAMAN_ASIMI",
                    $"Bilet #{biletId} odeme suresi doldu, iptal edildi.", GetClientIpAddress());

                TempData["Hata"] = "Odeme suresi doldu. Lutfen tekrar bilet alin.";
                return RedirectToAction("Index", "Sefer");
            }

            ViewBag.KalanSaniye = (int)timeRemaining.TotalSeconds;
            return View(ticket);
        }

        #endregion

        #region Tamamla (Complete Payment)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tamamla(
            int biletId,
            string odemeYontemi,
            string kartNo,
            string kartSahibi,
            string sonKullanma,
            string cvv)
        {
            int userId = GetCurrentUserId();

            var ticket = await _ticketService.GetTicketForUserAsync(userId, biletId);

            if (ticket == null)
            {
                TempData["Hata"] = "Bilet bulunamadi.";
                return RedirectToAction("Liste", "Bilet");
            }

            if (!ticket.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
                !ticket.Status.Equals("BEKLEMEDE", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Hata"] = "Gecersiz islem. Bilet durumu: " + ticket.Status;
                return RedirectToAction("Liste", "Bilet");
            }

            if (_paymentService.IsPaymentExpired(ticket.CreatedAt, PaymentTimeoutMinutes))
            {
                await _ticketService.CancelTicketAsync(biletId, userId);
                TempData["Hata"] = "Odeme suresi doldu.";
                return RedirectToAction("Index", "Sefer");
            }

            if (!_paymentService.ValidateCard(kartNo, sonKullanma, cvv))
            {
                TempData["Hata"] = "Kart bilgileri gecersiz.";
                return RedirectToAction("Odeme", new { biletId });
            }

            var referenceNo = _paymentService.GenerateReferenceNumber();

            var completed = await _ticketService.CompletePaymentAsync(biletId, userId);
            if (!completed)
            {
                TempData["Hata"] = "Odeme tamamlanamadi. Lutfen tekrar deneyin.";
                return RedirectToAction("Odeme", new { biletId });
            }

            await _logService.LogAsync(userId, "ODEME_TAMAMLA",
                $"Bilet #{biletId} odendi. Referans: {referenceNo}, Yontem: {odemeYontemi}",
                GetClientIpAddress());

            TempData["Basari"] = $"Odeme basarili! Referans: {referenceNo}";
            return RedirectToAction("Detay", "Bilet", new { id = biletId });
        }

        #endregion

        #region Basarisiz (Failed Payment)

        [HttpGet]
        public IActionResult Basarisiz(int biletId)
        {
            ViewBag.BiletId = biletId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TekrarDene(int biletId)
        {
            return RedirectToAction("Odeme", new { biletId });
        }

        #endregion

        #region KalanSure (AJAX)

        [HttpGet]
        public async Task<IActionResult> KalanSure(int biletId)
        {
            var ticket = await _ticketService.GetTicketForUserAsync(GetCurrentUserId(), biletId);

            if (ticket == null)
                return Json(new { expired = true, seconds = 0 });

            var timeRemaining = GetPaymentRemainingTime(ticket.CreatedAt);

            if (timeRemaining.TotalSeconds <= 0)
                return Json(new { expired = true, seconds = 0 });

            return Json(new { expired = false, seconds = (int)timeRemaining.TotalSeconds });
        }

        #endregion

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static TimeSpan GetPaymentRemainingTime(DateTime createdAt)
        {
            var createdAtUtc = createdAt.Kind switch
            {
                DateTimeKind.Utc => createdAt,
                DateTimeKind.Local => createdAt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(createdAt, DateTimeKind.Local).ToUniversalTime()
            };

            return createdAtUtc.AddMinutes(PaymentTimeoutMinutes) - DateTime.UtcNow;
        }

        #endregion
    }
}
