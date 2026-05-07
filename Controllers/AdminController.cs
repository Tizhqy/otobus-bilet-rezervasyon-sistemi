using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OtobusBiletRezervasyon.DTOs.Admin;
using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Services.FlowModels;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/[action]/{id?}")]
    public class AdminController : BaseController
    {
        private readonly IAdminFlowService _adminFlowService;

        public AdminController(IAdminFlowService adminFlowService)
        {
            _adminFlowService = adminFlowService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(string? depSearch, int depPage = 1, int busPage = 1, int routePage = 1)
        {
            var model = await _adminFlowService.GetDashboardAsync(depSearch, depPage, busPage, routePage);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetLiveBusLocations()
        {
            var buses = await _adminFlowService.GetOtobuslerAsync();
            var random = new Random();
            var locations = buses.Where(b => b.IsActive).Select((b, index) => new
            {
                Id = b.Id,
                Plate = b.PlateNumber,
                Lat = 39.5 + (random.NextDouble() * 2.0 - 1.0), // roughly central Turkey
                Lng = 34.0 + (random.NextDouble() * 5.0 - 2.5),
                Speed = random.Next(0, 10) > 2 ? random.Next(60, 100) : 0,
                Status = random.Next(0, 10) > 2 ? "Active" : "Stopped"
            });
            
            return Json(locations);
        }

        [HttpGet]
        public async Task<IActionResult> Otobusler()
        {
            var buses = await _adminFlowService.GetOtobuslerAsync();
            return View(buses);
        }

        [HttpGet]
        public IActionResult OtobusEkle() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtobusEkle(AdminBusDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _adminFlowService.OtobusEkleAsync(dto, GetCurrentUserId(), GetClientIpAddress());
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(dto);
            }

            TempData["Basari"] = result.Message;
            return RedirectToAction("Otobusler");
        }

        [HttpGet]
        public async Task<IActionResult> OtobusDuzenle(int id)
        {
            var bus = await _adminFlowService.GetOtobusByIdAsync(id);
            if (bus == null) return NotFound();
            return View(bus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtobusDuzenle(int id, AdminBusDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _adminFlowService.OtobusDuzenleAsync(id, dto, GetCurrentUserId(), GetClientIpAddress());
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(dto);
            }

            TempData["Basari"] = result.Message;
            return RedirectToAction("Otobusler");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtobusSil(int id)
        {
            var result = await _adminFlowService.OtobusDurumDegistirAsync(id, GetCurrentUserId(), GetClientIpAddress());
            SetResultToast(result);
            return RedirectToAction("Otobusler");
        }

        [HttpGet]
        public async Task<IActionResult> Rotalar()
        {
            var routes = await _adminFlowService.GetRotalarAsync();
            return View(routes);
        }

        [HttpGet]
        public async Task<IActionResult> RotaEkle()
        {
            ViewBag.Istasyonlar = await _adminFlowService.GetIstasyonSecenekleriAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RotaEkle(AdminRouteDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Istasyonlar = await _adminFlowService.GetIstasyonSecenekleriAsync();
                return View(dto);
            }

            var result = await _adminFlowService.RotaEkleAsync(dto, GetCurrentUserId(), GetClientIpAddress());
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.Istasyonlar = await _adminFlowService.GetIstasyonSecenekleriAsync();
                return View(dto);
            }

            TempData["Basari"] = result.Message;
            return RedirectToAction("Rotalar");
        }

        [HttpGet]
        public async Task<IActionResult> RotaDuzenle(int id)
        {
            var route = await _adminFlowService.GetRotaByIdAsync(id);
            if (route == null) return NotFound();

            ViewBag.Istasyonlar = await _adminFlowService.GetIstasyonSecenekleriAsync();
            return View(route);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RotaDuzenle(int id, AdminRouteDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Istasyonlar = await _adminFlowService.GetIstasyonSecenekleriAsync();
                return View(dto);
            }

            var result = await _adminFlowService.RotaDuzenleAsync(id, dto, GetCurrentUserId(), GetClientIpAddress());
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.Istasyonlar = await _adminFlowService.GetIstasyonSecenekleriAsync();
                return View(dto);
            }

            TempData["Basari"] = result.Message;
            return RedirectToAction("Rotalar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RotaSil(int id)
        {
            var result = await _adminFlowService.RotaDurumDegistirAsync(id, GetCurrentUserId(), GetClientIpAddress());
            SetResultToast(result);
            return RedirectToAction("Rotalar");
        }

        [HttpGet]
        public async Task<IActionResult> Istasyonlar()
        {
            var stations = await _adminFlowService.GetIstasyonlarAsync();
            return View(stations);
        }

        [HttpGet]
        public IActionResult IstasyonEkle() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IstasyonEkle(AdminStationDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _adminFlowService.IstasyonEkleAsync(dto, GetCurrentUserId(), GetClientIpAddress());
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(dto);
            }

            TempData["Basari"] = result.Message;
            return RedirectToAction("Istasyonlar");
        }

        [HttpGet]
        public async Task<IActionResult> IstasyonDuzenle(int id)
        {
            var station = await _adminFlowService.GetIstasyonByIdAsync(id);
            if (station == null) return NotFound();
            return View(station);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IstasyonDuzenle(int id, AdminStationDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _adminFlowService.IstasyonDuzenleAsync(id, dto, GetCurrentUserId(), GetClientIpAddress());
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(dto);
            }

            TempData["Basari"] = result.Message;
            return RedirectToAction("Istasyonlar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IstasyonSil(int id)
        {
            var result = await _adminFlowService.IstasyonDurumDegistirAsync(id, GetCurrentUserId(), GetClientIpAddress());
            SetResultToast(result);
            return RedirectToAction("Istasyonlar");
        }

        [HttpGet]
        public async Task<IActionResult> Seferler()
        {
            var departures = await _adminFlowService.GetSeferlerAsync();
            return View(departures);
        }

        [HttpGet]
        public async Task<IActionResult> SeferEkle()
        {
            var formData = await _adminFlowService.GetSeferFormDataAsync();
            ViewBag.Rotalar = formData.Rotalar;
            ViewBag.Otobusler = formData.Otobusler;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeferEkle(AdminDepartureDto dto)
        {
            if (!ModelState.IsValid)
            {
                var formData = await _adminFlowService.GetSeferFormDataAsync();
                ViewBag.Rotalar = formData.Rotalar;
                ViewBag.Otobusler = formData.Otobusler;
                return View(dto);
            }

            var result = await _adminFlowService.SeferEkleAsync(dto, GetCurrentUserId(), GetClientIpAddress());
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                var formData = await _adminFlowService.GetSeferFormDataAsync();
                ViewBag.Rotalar = formData.Rotalar;
                ViewBag.Otobusler = formData.Otobusler;
                return View(dto);
            }

            TempData["Basari"] = result.Message;
            return RedirectToAction("Seferler");
        }

        [HttpGet]
        public async Task<IActionResult> SeferDuzenle(int id)
        {
            var departure = await _adminFlowService.GetSeferByIdAsync(id);
            if (departure == null) return NotFound();

            var formData = await _adminFlowService.GetSeferFormDataAsync();
            ViewBag.Rotalar = formData.Rotalar;
            ViewBag.Otobusler = formData.Otobusler;
            return View(departure);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeferDuzenle(int id, AdminDepartureDto dto)
        {
            if (!ModelState.IsValid)
            {
                var formData = await _adminFlowService.GetSeferFormDataAsync();
                ViewBag.Rotalar = formData.Rotalar;
                ViewBag.Otobusler = formData.Otobusler;
                return View(dto);
            }

            var result = await _adminFlowService.SeferDuzenleAsync(id, dto, GetCurrentUserId(), GetClientIpAddress());
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                var formData = await _adminFlowService.GetSeferFormDataAsync();
                ViewBag.Rotalar = formData.Rotalar;
                ViewBag.Otobusler = formData.Otobusler;
                return View(dto);
            }

            TempData["Basari"] = result.Message;
            return RedirectToAction("Seferler");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeferPasife(int id)
        {
            var result = await _adminFlowService.SeferDurumDegistirAsync(id, GetCurrentUserId(), GetClientIpAddress());
            SetResultToast(result);
            return RedirectToAction("Seferler");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeferFiyatGuncelle(AdminSingleDeparturePriceUpdateDto request)
        {
            var result = await _adminFlowService.SeferFiyatGuncelleAsync(
                request,
                GetCurrentUserId(),
                GetClientIpAddress());

            SetResultToast(result);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeferFiyatTopluGuncelle(AdminBulkDeparturePriceUpdateDto request)
        {
            var result = await _adminFlowService.SeferFiyatTopluGuncelleAsync(
                request,
                GetCurrentUserId(),
                GetClientIpAddress());

            SetResultToast(result);
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Kullanicilar(string? ara, int sayfa = 1)
        {
            var model = await _adminFlowService.GetKullanicilarAsync(ara, sayfa);
            ViewBag.Ara = model.Ara;
            ViewBag.ToplamKayit = model.ToplamKayit;
            ViewBag.MevcutSayfa = model.MevcutSayfa;
            ViewBag.ToplamSayfa = model.ToplamSayfa;
            return View(model.Users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KullaniciRolDegistir(int kullaniciId, int roleId)
        {
            var result = await _adminFlowService.KullaniciRolDegistirAsync(
                GetCurrentUserId(),
                kullaniciId,
                roleId,
                GetClientIpAddress());

            SetResultToast(result);
            return RedirectToAction("Kullanicilar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KullaniciDurumDegistir(int id)
        {
            var result = await _adminFlowService.KullaniciDurumDegistirAsync(
                GetCurrentUserId(),
                id,
                GetClientIpAddress());

            SetResultToast(result);
            return RedirectToAction("Kullanicilar");
        }

        [HttpGet]
        public async Task<IActionResult> Kuponlar()
        {
            var kuponlar = await _adminFlowService.GetKuponlarAsync();
            return View(kuponlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KuponEkle(AdminCouponDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Hata"] = "Please check the form data and enter valid values.";
                return RedirectToAction("Kuponlar");
            }

            var result = await _adminFlowService.KuponEkleAsync(dto, GetCurrentUserId(), GetClientIpAddress());
            SetResultToast(result);
            return RedirectToAction("Kuponlar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KuponDurumDegistir(int id)
        {
            var result = await _adminFlowService.KuponDurumDegistirAsync(id, GetCurrentUserId(), GetClientIpAddress());
            SetResultToast(result);
            return RedirectToAction("Kuponlar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KuponSil(int id)
        {
            var result = await _adminFlowService.KuponSilAsync(id, GetCurrentUserId(), GetClientIpAddress());
            SetResultToast(result);
            return RedirectToAction("Kuponlar");
        }

        [HttpGet]
        public async Task<IActionResult> Loglar(string? islem, int? kullaniciId, int sayfa = 1)
        {
            var model = await _adminFlowService.GetLoglarAsync(islem, kullaniciId, sayfa);
            ViewBag.ToplamKayit = model.ToplamKayit;
            ViewBag.MevcutSayfa = model.MevcutSayfa;
            ViewBag.ToplamSayfa = model.ToplamSayfa;
            ViewBag.IslemFiltre = model.IslemFiltre;
            ViewBag.KullaniciFiltre = model.KullaniciFiltre;
            return View(model.Logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("AdminLogCleanupPolicy")]
        public async Task<IActionResult> LoglarTemizle(int gunSayisi = 30)
        {
            var result = await _adminFlowService.LoglarTemizleAsync(gunSayisi, GetCurrentUserId(), GetClientIpAddress());
            SetResultToast(result);
            return RedirectToAction("Loglar");
        }

        private void SetResultToast(ServiceResult result)
        {
            TempData[result.Success ? "Basari" : "Hata"] = result.Message;
        }
    }
}
