using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Services.Interfaces;
using Route = OtobusBiletRezervasyon.Models.Route;

namespace OtobusBiletRezervasyon.Controllers
{
    [Authorize(Roles = "Admin,admin")]
    [Microsoft.AspNetCore.Mvc.Route("Admin/[action]/{id?}")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogService _logService;

        public AdminController(IAdminService adminService, ILogService logService)
        {
            _adminService = adminService;
            _logService = logService;
        }

        #region Dashboard

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            var recentLogs = await _logService.GetRecentLogsAsync(10);

            ViewBag.Stats = stats;
            ViewBag.SonLoglar = recentLogs;

            return View();
        }

        #endregion

        #region Otobus Yonetimi (Bus Management)

        [HttpGet]
        public async Task<IActionResult> Otobusler()
        {
            var buses = await _adminService.GetAllBusesAsync();
            return View(buses);
        }

        [HttpGet]
        public IActionResult OtobusEkle() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtobusEkle(Bus bus)
        {
            if (!ModelState.IsValid)
                return View(bus);

            try
            {
                bus.PlateNumber = bus.PlateNumber?.ToUpper() ?? "";
                bus.IsActive = true;

                await _adminService.CreateBusAsync(bus);
                await LogAdminAction("OTOBUS_EKLE", $"Otobus eklendi: {bus.PlateNumber}");

                TempData["Basari"] = "Otobus eklendi.";
                return RedirectToAction("Otobusler");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(bus);
            }
        }

        [HttpGet]
        public async Task<IActionResult> OtobusDuzenle(int id)
        {
            var bus = await _adminService.GetBusByIdAsync(id);
            if (bus == null) return NotFound();
            return View(bus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtobusDuzenle(int id, Bus bus)
        {
            if (id != bus.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(bus);

            try
            {
                bus.PlateNumber = bus.PlateNumber?.ToUpper() ?? "";
                await _adminService.UpdateBusAsync(bus);
                await LogAdminAction("OTOBUS_DUZENLE", $"Otobus guncellendi: {bus.PlateNumber}");

                TempData["Basari"] = "Otobus guncellendi.";
                return RedirectToAction("Otobusler");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(bus);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtobusSil(int id)
        {
            var bus = await _adminService.GetBusByIdAsync(id);
            if (bus == null) return NotFound();

            var result = await _adminService.ToggleBusStatusAsync(id);

            if (result)
            {
                await LogAdminAction("OTOBUS_DURUM", $"Otobus durumu degistirildi: {bus.PlateNumber}");
                TempData["Basari"] = "Otobus durumu degistirildi.";
            }
            else
            {
                TempData["Hata"] = "Otobus durumu degistirilemedi.";
            }

            return RedirectToAction("Otobusler");
        }

        #endregion

        #region Rota Yonetimi (Route Management)

        [HttpGet]
        public async Task<IActionResult> Rotalar()
        {
            var routes = await _adminService.GetAllRoutesAsync();
            return View(routes);
        }

        [HttpGet]
        public async Task<IActionResult> RotaEkle()
        {
            ViewBag.Istasyonlar = await _adminService.GetAllStationsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RotaEkle(Route route)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Istasyonlar = await _adminService.GetAllStationsAsync();
                return View(route);
            }

            if (route.OriginStationId == route.DestinationStationId)
            {
                ModelState.AddModelError(string.Empty, "Kalkis ve varis istasyonlari ayni olamaz.");
                ViewBag.Istasyonlar = await _adminService.GetAllStationsAsync();
                return View(route);
            }

            try
            {
                route.IsActive = true;
                await _adminService.CreateRouteAsync(route);
                await LogAdminAction("ROTA_EKLE", $"Rota #{route.Id} eklendi");

                TempData["Basari"] = "Rota eklendi.";
                return RedirectToAction("Rotalar");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Istasyonlar = await _adminService.GetAllStationsAsync();
                return View(route);
            }
        }

        [HttpGet]
        public async Task<IActionResult> RotaDuzenle(int id)
        {
            var route = await _adminService.GetRouteByIdAsync(id);
            if (route == null) return NotFound();

            ViewBag.Istasyonlar = await _adminService.GetAllStationsAsync();
            return View(route);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RotaDuzenle(int id, Route route)
        {
            if (id != route.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.Istasyonlar = await _adminService.GetAllStationsAsync();
                return View(route);
            }

            try
            {
                await _adminService.UpdateRouteAsync(route);
                await LogAdminAction("ROTA_DUZENLE", $"Rota #{id} guncellendi");

                TempData["Basari"] = "Rota guncellendi.";
                return RedirectToAction("Rotalar");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Istasyonlar = await _adminService.GetAllStationsAsync();
                return View(route);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RotaSil(int id)
        {
            var result = await _adminService.ToggleRouteStatusAsync(id);

            if (result)
            {
                await LogAdminAction("ROTA_DURUM", $"Rota #{id} durumu degistirildi");
                TempData["Basari"] = "Rota durumu degistirildi.";
            }
            else
            {
                TempData["Hata"] = "Rota bulunamadi.";
            }

            return RedirectToAction("Rotalar");
        }

        #endregion

        #region Istasyon Yonetimi (Station Management)

        [HttpGet]
        public async Task<IActionResult> Istasyonlar()
        {
            var stations = await _adminService.GetAllStationsAsync();
            return View(stations);
        }

        [HttpGet]
        public IActionResult IstasyonEkle() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IstasyonEkle(Station station)
        {
            if (!ModelState.IsValid)
                return View(station);

            try
            {
                station.IsActive = true;
                await _adminService.CreateStationAsync(station);
                await LogAdminAction("ISTASYON_EKLE", $"Istasyon eklendi: {station.Name}, {station.City}");

                TempData["Basari"] = "Istasyon eklendi.";
                return RedirectToAction("Istasyonlar");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(station);
            }
        }

        [HttpGet]
        public async Task<IActionResult> IstasyonDuzenle(int id)
        {
            var station = await _adminService.GetStationByIdAsync(id);
            if (station == null) return NotFound();
            return View(station);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IstasyonDuzenle(int id, Station station)
        {
            if (id != station.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(station);

            try
            {
                await _adminService.UpdateStationAsync(station);
                await LogAdminAction("ISTASYON_DUZENLE", $"Istasyon #{id} guncellendi");

                TempData["Basari"] = "Istasyon guncellendi.";
                return RedirectToAction("Istasyonlar");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(station);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IstasyonSil(int id)
        {
            var result = await _adminService.ToggleStationStatusAsync(id);

            if (result)
            {
                await LogAdminAction("ISTASYON_DURUM", $"Istasyon #{id} durumu degistirildi");
                TempData["Basari"] = "Istasyon durumu degistirildi.";
            }
            else
            {
                TempData["Hata"] = "Istasyon bulunamadi.";
            }

            return RedirectToAction("Istasyonlar");
        }

        #endregion

        #region Sefer Yonetimi (Departure Management)

        [HttpGet]
        public async Task<IActionResult> Seferler()
        {
            var departures = await _adminService.GetAllDeparturesAsync();
            return View(departures);
        }

        [HttpGet]
        public async Task<IActionResult> SeferEkle()
        {
            ViewBag.Rotalar = await _adminService.GetAllRoutesAsync();
            ViewBag.Otobusler = await _adminService.GetAllBusesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeferEkle(Departure departure)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Rotalar = await _adminService.GetAllRoutesAsync();
                ViewBag.Otobusler = await _adminService.GetAllBusesAsync();
                return View(departure);
            }

            if (departure.DepartureTime <= DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Kalkis tarihi gecmis olamaz.");
                ViewBag.Rotalar = await _adminService.GetAllRoutesAsync();
                ViewBag.Otobusler = await _adminService.GetAllBusesAsync();
                return View(departure);
            }

            if (departure.ArrivalTime <= departure.DepartureTime)
            {
                ModelState.AddModelError(string.Empty, "Varis tarihi kalkis tarihinden sonra olmalidir.");
                ViewBag.Rotalar = await _adminService.GetAllRoutesAsync();
                ViewBag.Otobusler = await _adminService.GetAllBusesAsync();
                return View(departure);
            }

            try
            {
                departure.IsActive = true;
                var created = await _adminService.CreateDepartureAsync(departure);
                await LogAdminAction("SEFER_EKLE", $"Sefer #{created.Id} eklendi");

                TempData["Basari"] = "Sefer eklendi.";
                return RedirectToAction("Seferler");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Rotalar = await _adminService.GetAllRoutesAsync();
                ViewBag.Otobusler = await _adminService.GetAllBusesAsync();
                return View(departure);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SeferDuzenle(int id)
        {
            var departure = await _adminService.GetDepartureByIdAsync(id);
            if (departure == null) return NotFound();

            ViewBag.Rotalar = await _adminService.GetAllRoutesAsync();
            ViewBag.Otobusler = await _adminService.GetAllBusesAsync();
            return View(departure);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeferDuzenle(int id, Departure departure)
        {
            if (id != departure.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.Rotalar = await _adminService.GetAllRoutesAsync();
                ViewBag.Otobusler = await _adminService.GetAllBusesAsync();
                return View(departure);
            }

            try
            {
                await _adminService.UpdateDepartureAsync(departure);
                await LogAdminAction("SEFER_DUZENLE", $"Sefer #{id} guncellendi");

                TempData["Basari"] = "Sefer guncellendi.";
                return RedirectToAction("Seferler");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Rotalar = await _adminService.GetAllRoutesAsync();
                ViewBag.Otobusler = await _adminService.GetAllBusesAsync();
                return View(departure);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeferPasife(int id)
        {
            var result = await _adminService.ToggleDepartureStatusAsync(id);

            if (result)
            {
                await LogAdminAction("SEFER_DURUM", $"Sefer #{id} durumu degistirildi");
                TempData["Basari"] = "Sefer durumu degistirildi.";
            }
            else
            {
                TempData["Hata"] = "Sefer bulunamadi.";
            }

            return RedirectToAction("Seferler");
        }

        #endregion

        #region Kullanici Yonetimi (User Management)

        [HttpGet]
        public async Task<IActionResult> Kullanicilar(string? ara)
        {
            var users = await _adminService.GetAllUsersAsync();

            if (!string.IsNullOrWhiteSpace(ara))
            {
                users = users.Where(u =>
                    u.FirstName.Contains(ara, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(ara, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(ara, StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.Ara = ara;
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KullaniciRolDegistir(int kullaniciId, int roleId)
        {
            int adminId = GetCurrentUserId();

            if (kullaniciId == adminId)
            {
                TempData["Hata"] = "Kendi rolunuzu degistiremezsiniz.";
                return RedirectToAction("Kullanicilar");
            }

            var user = await _adminService.GetUserByIdAsync(kullaniciId);
            if (user == null)
            {
                TempData["Hata"] = "Kullanici bulunamadi.";
                return RedirectToAction("Kullanicilar");
            }

            user.RoleId = roleId;

            try
            {
                await _adminService.UpdateUserAsync(user);
                await LogAdminAction("ROL_DEGISTIR", $"Kullanici #{kullaniciId} rolu #{roleId} yapildi");

                TempData["Basari"] = "Rol guncellendi.";
            }
            catch (Exception)
            {
                TempData["Hata"] = "Rol guncellenirken hata olustu.";
            }

            return RedirectToAction("Kullanicilar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KullaniciDurumDegistir(int id)
        {
            int adminId = GetCurrentUserId();

            if (id == adminId)
            {
                TempData["Hata"] = "Kendi hesabinizi pasife alamazsiniz.";
                return RedirectToAction("Kullanicilar");
            }

            var result = await _adminService.ToggleUserStatusAsync(id);

            if (result)
            {
                await LogAdminAction("KULLANICI_DURUM", $"Kullanici #{id} durumu degistirildi");
                TempData["Basari"] = "Kullanici durumu degistirildi.";
            }
            else
            {
                TempData["Hata"] = "Kullanici bulunamadi.";
            }

            return RedirectToAction("Kullanicilar");
        }

        #endregion

        #region Log Yonetimi (Log Management)

        [HttpGet]
        public async Task<IActionResult> Loglar(string? islem, int? kullaniciId, int sayfa = 1)
        {
            int sayfaBoyutu = 50;
            IEnumerable<Log> logs;

            if (!string.IsNullOrWhiteSpace(islem))
            {
                logs = await _logService.GetLogsByActionAsync(islem);
            }
            else if (kullaniciId.HasValue)
            {
                logs = await _logService.GetLogsByUserIdAsync(kullaniciId.Value);
            }
            else
            {
                logs = await _logService.GetLogsAsync();
            }

            var logList = logs.ToList();
            int toplamKayit = logList.Count;

            var pagedLogs = logList
                .OrderByDescending(l => l.CreatedAt)
                .Skip((sayfa - 1) * sayfaBoyutu)
                .Take(sayfaBoyutu)
                .ToList();

            ViewBag.ToplamKayit = toplamKayit;
            ViewBag.MevcutSayfa = sayfa;
            ViewBag.ToplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu);
            ViewBag.IslemFiltre = islem;
            ViewBag.KullaniciFiltre = kullaniciId;

            return View(pagedLogs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoglarTemizle(int gunSayisi = 30)
        {
            if (gunSayisi < 7)
            {
                TempData["Hata"] = "En az 7 gunluk loglar saklanmalidir.";
                return RedirectToAction("Loglar");
            }

            var result = await _logService.DeleteOldLogsAsync(gunSayisi);

            if (result)
            {
                await LogAdminAction("LOG_TEMIZLE", $"{gunSayisi} gunden eski loglar silindi");
                TempData["Basari"] = $"{gunSayisi} gunden eski loglar temizlendi.";
            }
            else
            {
                TempData["Hata"] = "Log temizleme islemi basarisiz.";
            }

            return RedirectToAction("Loglar");
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

        private async Task LogAdminAction(string action, string description)
        {
            int adminId = GetCurrentUserId();
            await _logService.LogAdminActionAsync(adminId, action, description, GetClientIpAddress());
        }

        #endregion
    }
}
