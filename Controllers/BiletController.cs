using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtobusBiletRezervasyon.DTOs.Ticket;
using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Services.FlowModels;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Controllers
{
    [Authorize]
    public class BiletController : BaseController
    {
        private readonly IBiletFlowService _biletFlowService;

        public BiletController(IBiletFlowService biletFlowService)
        {
            _biletFlowService = biletFlowService;
        }

        #region Liste (User Tickets)

        [HttpGet]
        public async Task<IActionResult> Liste()
        {
            int userId = GetCurrentUserId();
            var tickets = await _biletFlowService.GetUserTicketsAsync(userId);
            return View(tickets);
        }

        #endregion

        #region Detay (Ticket Detail)

        [HttpGet]
        public async Task<IActionResult> Detay(int id)
        {
            int userId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
            var result = await _biletFlowService.GetTicketDetayForUserAsync(id, userId, isAdmin);

            if (!result.Success)
            {
                return result.Type switch
                {
                    ServiceResultType.NotFound => NotFound(),
                    ServiceResultType.Forbidden => Forbid(),
                    _ => NotFound()
                };
            }

            return View(result.Data);
        }

        #endregion

        #region SatinAl (Purchase)

        [HttpGet]
        public async Task<IActionResult> SatinAl(int seferId, [FromQuery] int[] koltukIds)
        {
            var result = await _biletFlowService.HazirlaSatinAlSayfasiAsync(seferId, koltukIds);

            if (!result.Success)
            {
                TempData["Hata"] = result.Message;
                if (result.Type == ServiceResultType.Conflict && seferId > 0)
                    return RedirectToAction("Detay", "Sefer", new { id = seferId });
                if (result.Type == ServiceResultType.Expired)
                    return RedirectToAction("Index", "Sefer");

                return RedirectToAction("Index", "Sefer");
            }

            SetSatinAlViewBag(result.Data!);
            return View(result.Data!.Form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SatinAl(CreateTicketDto createTicketDto)
        {
            if (!ModelState.IsValid)
            {
                var pageResult = await _biletFlowService.HazirlaSatinAlSayfasiAsync(createTicketDto);
                if (pageResult.Success)
                    SetSatinAlViewBag(pageResult.Data!);
                return View(createTicketDto);
            }

            int userId = GetCurrentUserId();
            var result = await _biletFlowService.SatinAlAsync(userId, createTicketDto);

            if (!result.Success)
            {
                TempData["Hata"] = result.Message;
                if (result.Type == ServiceResultType.NotFound)
                    return RedirectToAction("Index", "Sefer");
                if (result.Type == ServiceResultType.Expired)
                    return RedirectToAction("Index", "Sefer");
                if (result.Type == ServiceResultType.ValidationError)
                {
                    var selectedSeatIds = createTicketDto.Passengers?.Select(p => p.SeatId).ToArray() ?? Array.Empty<int>();
                    return RedirectToAction("SatinAl", new { seferId = createTicketDto.DepartureId, koltukIds = selectedSeatIds });
                }

                return RedirectToAction("Detay", "Sefer", new { id = createTicketDto.DepartureId });
            }

            return RedirectToAction("Odeme", "Odeme", new { biletId = result.Data!.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SatinAlForm(
            int seferId,
            int koltukId,
            string yolcuAd,
            string yolcuSoyad,
            string? yolcuTc,
            string odemeYontemi = "CreditCard")
        {
            int userId = GetCurrentUserId();
            var result = await _biletFlowService.SatinAlFormAsync(
                userId, seferId, koltukId, yolcuAd, yolcuSoyad, yolcuTc, odemeYontemi);

            if (!result.Success)
            {
                TempData["Hata"] = result.Message;

                return result.Type switch
                {
                    ServiceResultType.NotFound => RedirectToAction("Index", "Sefer"),
                    ServiceResultType.Conflict => RedirectToAction("Detay", "Sefer", new { id = seferId }),
                    ServiceResultType.Expired => RedirectToAction("Index", "Sefer"),
                    _ => RedirectToAction("SatinAl", new { seferId, koltukId })
                };
            }

            return RedirectToAction("Odeme", "Odeme", new { biletId = result.Data!.Id });
        }

        #endregion

        #region Iptal (Cancel)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Iptal(int id)
        {
            int userId = GetCurrentUserId();
            var result = await _biletFlowService.IptalAsync(id, userId);

            if (!result.Success)
            {
                if (result.Type == ServiceResultType.NotFound)
                    return NotFound();
                if (result.Type == ServiceResultType.Forbidden)
                    return Forbid();

                TempData["Hata"] = result.Message;
                return RedirectToAction("Detay", new { id });
            }

            TempData["Basari"] = result.Message;
            return RedirectToAction("Liste");
        }

        #endregion

        #region Koltuk Kontrolu (AJAX)

        [HttpGet]
        public async Task<IActionResult> KoltukMusaitMi(int seferId, int koltukId)
        {
            var isAvailable = await _biletFlowService.KoltukMusaitMiAsync(seferId, koltukId);
            return Json(new { available = isAvailable });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KoltukKontrol(int seferId, [FromBody] List<int> koltukIds)
        {
            if (koltukIds == null || !koltukIds.Any())
                return BadRequest(new { error = "Koltuk secilmedi." });

            var areAvailable = await _biletFlowService.KoltuklarMusaitMiAsync(seferId, koltukIds);
            return Json(new { available = areAvailable });
        }

        #endregion

        private void SetSatinAlViewBag(BiletSatinAlViewModel model)
        {
            ViewBag.Sefer = model.Sefer;
            ViewBag.SecilenKoltuklar = model.SecilenKoltuklar;
            ViewBag.SeferId = model.SeferId;
            ViewBag.KoltukIds = model.KoltukIds;
        }
    }
}
