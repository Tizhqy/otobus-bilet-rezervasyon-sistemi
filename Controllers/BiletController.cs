using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OtobusBiletRezervasyon.DTOs.Ticket;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Controllers
{
    [Authorize]
    public class BiletController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly ISearchService _searchService;
        private readonly ILogService _logService;

        public BiletController(
            ITicketService ticketService,
            ISearchService searchService,
            ILogService logService)
        {
            _ticketService = ticketService;
            _searchService = searchService;
            _logService = logService;
        }

        #region Liste (User Tickets)

        [HttpGet]
        public async Task<IActionResult> Liste()
        {
            int userId = GetCurrentUserId();
            var tickets = await _ticketService.GetUserTicketsAsync(userId);
            return View(tickets);
        }

        #endregion

        #region Detay (Ticket Detail)

        [HttpGet]
        public async Task<IActionResult> Detay(int id)
        {
            if (id <= 0)
                return NotFound();

            var ticket = await _ticketService.GetTicketByIdAsync(id);

            if (ticket == null)
                return NotFound();

            return View(ticket);
        }

        #endregion

        #region SatinAl (Purchase)

        [HttpGet]
        public async Task<IActionResult> SatinAl(int seferId, int koltukId)
        {
            if (seferId <= 0 || koltukId <= 0)
            {
                TempData["Hata"] = "Gecersiz sefer veya koltuk bilgisi.";
                return RedirectToAction("Index", "Sefer");
            }

            var isAvailable = await _ticketService.IsSeatAvailableAsync(seferId, koltukId);

            if (!isAvailable)
            {
                TempData["Hata"] = "Bu koltuk dolu. Lutfen baska bir koltuk secin.";
                return RedirectToAction("Detay", "Sefer", new { id = seferId });
            }

            var departure = await _searchService.GetDepartureByIdAsync(seferId);

            if (departure == null)
            {
                TempData["Hata"] = "Sefer bulunamadi.";
                return RedirectToAction("Index", "Sefer");
            }

            if (departure.DepartureTime <= DateTime.UtcNow)
            {
                TempData["Hata"] = "Bu sefer icin bilet satisi sona ermistir.";
                return RedirectToAction("Index", "Sefer");
            }

            var seats = await _searchService.GetSeatsForDepartureAsync(seferId);
            var selectedSeat = seats.FirstOrDefault(s => s.Id == koltukId);

            ViewBag.Sefer = departure;
            ViewBag.SecilenKoltuk = selectedSeat;
            ViewBag.SeferId = seferId;
            ViewBag.KoltukId = koltukId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SatinAl(CreateTicketDto createTicketDto)
        {
            int userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                var departure = await _searchService.GetDepartureByIdAsync(createTicketDto.DepartureId);
                ViewBag.Sefer = departure;
                ViewBag.SeferId = createTicketDto.DepartureId;
                return View(createTicketDto);
            }

            try
            {
                var ticket = await _ticketService.PurchaseTicketAsync(userId, createTicketDto);
                await _logService.LogTicketPurchaseAsync(userId, ticket.Id, GetClientIpAddress());
                return RedirectToAction("Odeme", "Odeme", new { biletId = ticket.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Hata"] = ex.Message;
                return RedirectToAction("Detay", "Sefer", new { id = createTicketDto.DepartureId });
            }
            catch (Exception)
            {
                TempData["Hata"] = "Bilet satin alinirken bir hata olustu. Lutfen tekrar deneyin.";
                return RedirectToAction("Detay", "Sefer", new { id = createTicketDto.DepartureId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SatinAlForm(
            int seferId,
            int koltukId,
            string yolcuAd,
            string yolcuSoyad,
            string? yolcuTc,
            string odemeYontemi = "credit_card")
        {
            int userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(yolcuAd) || string.IsNullOrWhiteSpace(yolcuSoyad))
            {
                TempData["Hata"] = "Yolcu adi ve soyadi zorunludur.";
                return RedirectToAction("SatinAl", new { seferId, koltukId });
            }

            var isAvailable = await _ticketService.IsSeatAvailableAsync(seferId, koltukId);

            if (!isAvailable)
            {
                TempData["Hata"] = "Bu koltuk artik musait degil.";
                return RedirectToAction("Detay", "Sefer", new { id = seferId });
            }

            var createTicketDto = new CreateTicketDto
            {
                DepartureId = seferId,
                Passengers = new List<PassengerDto>
                {
                    new PassengerDto
                    {
                        SeatId = koltukId,
                        FirstName = yolcuAd,
                        LastName = yolcuSoyad,
                        IdNumber = yolcuTc
                    }
                },
                Payment = new PaymentInfoDto
                {
                    Method = odemeYontemi,
                    TransactionId = Guid.NewGuid().ToString("N")
                }
            };

            try
            {
                var ticket = await _ticketService.PurchaseTicketAsync(userId, createTicketDto);
                await _logService.LogTicketPurchaseAsync(userId, ticket.Id, GetClientIpAddress());
                return RedirectToAction("Odeme", "Odeme", new { biletId = ticket.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Hata"] = ex.Message;
                return RedirectToAction("Detay", "Sefer", new { id = seferId });
            }
            catch (Exception)
            {
                TempData["Hata"] = "Bilet satin alinirken bir hata olustu.";
                return RedirectToAction("Detay", "Sefer", new { id = seferId });
            }
        }

        #endregion

        #region Iptal (Cancel)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Iptal(int id)
        {
            if (id <= 0)
                return NotFound();

            int userId = GetCurrentUserId();

            var success = await _ticketService.CancelTicketAsync(id, userId);

            if (!success)
            {
                TempData["Hata"] = "Bilet iptal edilemedi. Bilet bulunamadi, zaten iptal edilmis veya kalkisa 2 saatten az kalmis olabilir.";
                return RedirectToAction("Detay", new { id });
            }

            await _logService.LogTicketCancellationAsync(userId, id, GetClientIpAddress());

            TempData["Basari"] = "Biletiniz iptal edildi.";
            return RedirectToAction("Liste");
        }

        #endregion

        #region Koltuk Kontrolu (AJAX)

        [HttpGet]
        public async Task<IActionResult> KoltukMusaitMi(int seferId, int koltukId)
        {
            var isAvailable = await _ticketService.IsSeatAvailableAsync(seferId, koltukId);
            return Json(new { available = isAvailable });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KoltukKontrol(int seferId, [FromBody] List<int> koltukIds)
        {
            if (koltukIds == null || !koltukIds.Any())
                return BadRequest(new { error = "Koltuk secilmedi." });

            var areAvailable = await _ticketService.AreSeatAvailableAsync(seferId, koltukIds);
            return Json(new { available = areAvailable });
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

        #endregion
    }
}
