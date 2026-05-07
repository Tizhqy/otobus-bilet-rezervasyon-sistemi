using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OtobusBiletRezervasyon.DTOs.Search;
using OtobusBiletRezervasyon.Services.FlowModels;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Controllers
{
    public class SeferController : BaseController
    {
        private readonly ISeferFlowService _seferFlowService;

        public SeferController(ISeferFlowService seferFlowService)
        {
            _seferFlowService = seferFlowService;
        }

        #region Index (Ana Sayfa)

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _seferFlowService.GetIndexDataAsync();
            ViewBag.Istasyonlar = data.Istasyonlar;
            ViewBag.YaklasanSeferler = data.YaklasanSeferler;
            return View(data.AramaFormu);
        }

        [HttpGet]
        public async Task<IActionResult> AramaSonuclari()
        {
            var data = await _seferFlowService.GetBosAramaSayfasiAsync();
            ViewBag.Istasyonlar = data.Istasyonlar;
            ViewBag.AramaKriterleri = data.AramaKriterleri;
            return View(data);
        }

        #endregion

        #region Ara (Search)

        [HttpGet]
        public async Task<IActionResult> Ara(SearchQueryDto searchQuery)
        {
            var result = await _seferFlowService.AraAsync(searchQuery);
            if (!result.Success)
            {
                TempData["Hata"] = result.Message;
                return RedirectToAction("Index");
            }

            ViewBag.AramaKriterleri = result.Data!.AramaKriterleri;
            ViewBag.KalkisIstasyonu = result.Data.KalkisIstasyonu;
            ViewBag.VarisIstasyonu = result.Data.VarisIstasyonu;
            ViewBag.Istasyonlar = result.Data.Istasyonlar;
            return View("AramaSonuclari", result.Data);
        }

        #endregion

        #region Detay (Departure Detail)

        [HttpGet]
        public async Task<IActionResult> Detay(int id)
        {
            var result = await _seferFlowService.GetDetayAsync(id);
            if (!result.Success)
            {
                if (result.Type == ServiceResultType.NotFound)
                    return NotFound();

                TempData["Hata"] = result.Message;
                return RedirectToAction("Index");
            }

            return View(result.Data);
        }

        #endregion

        #region KoltukDurumu (Seat Map - AJAX)

        [HttpGet]
        public async Task<IActionResult> KoltukDurumu(int seferId)
        {
            var result = await _seferFlowService.GetKoltukDurumuAsync(seferId);
            if (!result.Success)
                return BadRequest(result.Message);

            return PartialView("_KoltukHaritasi", result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> MusaitKoltuklar(int seferId)
        {
            var result = await _seferFlowService.GetMusaitKoltuklarAsync(seferId);
            if (!result.Success)
                return BadRequest(new { error = result.Message });

            return Json(result.Data);
        }

        #endregion

        #region IstasyonAra (Station Search - AJAX)

        [HttpGet]
        [OutputCache(PolicyName = "StationSearchCache")]
        public async Task<IActionResult> IstasyonAra(string query)
        {
            var normalizedQuery = (query ?? string.Empty).Trim();
            if (normalizedQuery.Length > AppConfig.MaxStationSearchQueryLength)
                return Json(Array.Empty<StationInfoDto>());

            var stations = await _seferFlowService.IstasyonAraAsync(normalizedQuery);
            return Json(stations);
        }

        [HttpGet]
        [OutputCache(PolicyName = "StationListCache")]
        public async Task<IActionResult> TumIstasyonlar()
        {
            var stations = await _seferFlowService.TumIstasyonlarAsync();
            return Json(stations);
        }

        #endregion

        #region YaklasanSeferler (Upcoming Departures)

        [HttpGet]
        public async Task<IActionResult> YaklasanSeferler(int count = 10)
        {
            count = Math.Clamp(count, 1, AppConfig.MaxUpcomingDepartureCount);
            var departures = await _seferFlowService.GetYaklasanSeferlerAsync(count);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(departures);

            return PartialView("_YaklasanSeferler", departures);
        }

        #endregion
    }
}
