using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Services.FlowModels;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Controllers
{
    [Authorize]
    public class OdemeController : BaseController
    {
        private readonly IOdemeFlowService _odemeFlowService;

        public OdemeController(IOdemeFlowService odemeFlowService)
        {
            _odemeFlowService = odemeFlowService;
        }

        #region Odeme (Payment Page)

        [HttpGet]
        public async Task<IActionResult> Odeme(int biletId)
        {
            int userId = GetCurrentUserId();
            var result = await _odemeFlowService.HazirlaOdemeSayfasiAsync(biletId, userId);

            if (!result.Success)
            {
                TempData["Hata"] = result.Message;
                return result.Type switch
                {
                    ServiceResultType.NotFound => RedirectToAction("Liste", "Bilet"),
                    ServiceResultType.Forbidden => RedirectToAction("Liste", "Bilet"),
                    ServiceResultType.Expired => RedirectToAction("Index", "Sefer"),
                    _ => RedirectToAction("Liste", "Bilet")
                };
            }

            ViewBag.KalanSaniye = result.Data!.KalanSaniye;
            return View(result.Data.Ticket);
        }

        #endregion

        #region Tamamla (Complete Payment)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tamamla(OdemeTamamlamaIstekDto request)
        {
            if (!ModelState.IsValid)
            {
                TempData["Hata"] = "Invalid payment details.";
                return RedirectToAction("Odeme", new { biletId = request.BiletId });
            }

            int userId = GetCurrentUserId();
            var result = await _odemeFlowService.OdemeyiTamamlaAsync(
                request.BiletId, userId, request.OdemeYontemi, request.PaymentToken, request.IdempotencyKey, request.CardLast4, request.CouponCode);

            if (!result.Success)
            {
                TempData["Hata"] = result.Message;

                return result.Type switch
                {
                    ServiceResultType.NotFound => RedirectToAction("Liste", "Bilet"),
                    ServiceResultType.Forbidden => RedirectToAction("Liste", "Bilet"),
                    ServiceResultType.Expired => RedirectToAction("Index", "Sefer"),
                    ServiceResultType.ValidationError => RedirectToAction("Odeme", new { biletId = request.BiletId }),
                    _ => RedirectToAction("Liste", "Bilet")
                };
            }

            TempData["Basari"] = $"Payment successful! Reference: {result.Data!.ReferenceNo}";
            return RedirectToAction("Detay", "Bilet", new { id = request.BiletId });
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
            int userId = GetCurrentUserId();
            var state = await _odemeFlowService.KalanSureAsync(biletId, userId);
            if (!state.authorized)
                return Unauthorized();

            return Json(new { expired = state.expired, seconds = state.seconds });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UygulaKupon([FromBody] UygulaKuponIstekDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request");

            int userId = GetCurrentUserId();
            var result = await _odemeFlowService.UygulaKuponAsync(request.BiletId, userId, request.KuponKodu);

            if (!result.Success)
                return BadRequest(result.Message);

            return Json(new { success = true, newPrice = result.Data });
        }

        #endregion
    }
}
