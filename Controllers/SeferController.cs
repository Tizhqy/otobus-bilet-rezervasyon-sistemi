using Microsoft.AspNetCore.Mvc;
using OtobusBiletRezervasyon.DTOs.Search;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Controllers
{
    public class SeferController : Controller
    {
        private readonly ISearchService _searchService;

        public SeferController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        #region Index (Ana Sayfa)

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var stations = await _searchService.GetAllStationsAsync();
            var upcomingDepartures = await _searchService.GetUpcomingDeparturesAsync(10);

            ViewBag.Istasyonlar = stations;
            ViewBag.YaklasanSeferler = upcomingDepartures;

            return View();
        }

        #endregion

        #region Ara (Search)

        [HttpGet]
        public async Task<IActionResult> Ara(SearchQueryDto searchQuery)
        {
            if (searchQuery.OriginStationId <= 0 || searchQuery.DestinationStationId <= 0)
            {
                TempData["Hata"] = "Lutfen kalkis ve varis istasyonlarini secin.";
                return RedirectToAction("Index");
            }

            if (searchQuery.OriginStationId == searchQuery.DestinationStationId)
            {
                TempData["Hata"] = "Kalkis ve varis istasyonlari ayni olamaz.";
                return RedirectToAction("Index");
            }

            if (searchQuery.TravelDate.Date < DateTime.Today)
            {
                TempData["Hata"] = "Gecmis tarih secilemez.";
                return RedirectToAction("Index");
            }

            if (searchQuery.PassengerCount <= 0)
                searchQuery.PassengerCount = 1;

            var departures = await _searchService.SearchDeparturesAsync(searchQuery);

            var allStations = await _searchService.GetAllStationsAsync();
            var originStation = allStations.FirstOrDefault(s => s.Id == searchQuery.OriginStationId);
            var destinationStation = allStations.FirstOrDefault(s => s.Id == searchQuery.DestinationStationId);

            ViewBag.AramaKriterleri = searchQuery;
            ViewBag.KalkisIstasyonu = originStation;
            ViewBag.VarisIstasyonu = destinationStation;
            ViewBag.Istasyonlar = allStations;

            return View("AramaSonuclari", departures);
        }

        #endregion

        #region Detay (Departure Detail)

        [HttpGet]
        public async Task<IActionResult> Detay(int id)
        {
            if (id <= 0)
                return NotFound();

            var departure = await _searchService.GetDepartureByIdAsync(id);

            if (departure == null)
                return NotFound();

            if (departure.DepartureTime <= DateTime.Now)
            {
                TempData["Hata"] = "Bu sefer icin bilet satisi sona ermistir.";
                return RedirectToAction("Index");
            }

            var seats = await _searchService.GetSeatsForDepartureAsync(id);

            ViewBag.Koltuklar = seats;
            return View(departure);
        }

        #endregion

        #region KoltukDurumu (Seat Map - AJAX)

        [HttpGet]
        public async Task<IActionResult> KoltukDurumu(int seferId)
        {
            if (seferId <= 0)
                return BadRequest("Gecersiz sefer ID.");

            var seats = await _searchService.GetSeatsForDepartureAsync(seferId);
            return PartialView("_KoltukHaritasi", seats);
        }

        [HttpGet]
        public async Task<IActionResult> MusaitKoltuklar(int seferId)
        {
            if (seferId <= 0)
                return BadRequest(new { error = "Gecersiz sefer ID." });

            var seats = await _searchService.GetAvailableSeatsForDepartureAsync(seferId);
            return Json(seats);
        }

        #endregion

        #region IstasyonAra (Station Search - AJAX)

        [HttpGet]
        public async Task<IActionResult> IstasyonAra(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(new List<StationInfoDto>());

            var stations = await _searchService.SearchStationsAsync(query);
            return Json(stations);
        }

        [HttpGet]
        public async Task<IActionResult> TumIstasyonlar()
        {
            var stations = await _searchService.GetAllStationsAsync();
            return Json(stations);
        }

        #endregion

        #region YaklasanSeferler (Upcoming Departures)

        [HttpGet]
        public async Task<IActionResult> YaklasanSeferler(int count = 10)
        {
            if (count <= 0) count = 10;
            if (count > 50) count = 50;

            var departures = await _searchService.GetUpcomingDeparturesAsync(count);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(departures);

            return PartialView("_YaklasanSeferler", departures);
        }

        #endregion
    }
}
