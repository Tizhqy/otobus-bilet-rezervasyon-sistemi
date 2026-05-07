using OtobusBiletRezervasyon.DTOs.Search;
using OtobusBiletRezervasyon.DTOs.ViewModels;
using OtobusBiletRezervasyon.Services.FlowModels;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class SeferFlowService : ISeferFlowService
    {
        private readonly ISearchService _searchService;

        public SeferFlowService(ISearchService searchService)
        {
            _searchService = searchService;
        }

        public async Task<SeferIndexViewModel> GetIndexDataAsync()
        {
            var maxPrice = await _searchService.GetMaxPriceForUpcomingDeparturesAsync();
            
            return new SeferIndexViewModel
            {
                Istasyonlar = await _searchService.GetAllStationsAsync(),
                YaklasanSeferler = await _searchService.GetUpcomingDeparturesAsync(AppConfig.DefaultUpcomingDepartureCount),
                AramaFormu = new SearchQueryDto
                {
                    TravelDate = DateTime.Today.AddDays(1),
                    PassengerCount = 1
                },
                MaxPrice = maxPrice
            };
        }

        public async Task<SeferAramaContextViewModel> GetBosAramaSayfasiAsync()
        {
            var upcomingDepartures = await _searchService.GetUpcomingDeparturesAsync(AppConfig.DefaultUpcomingDepartureCount);
            var maxPrice = await _searchService.GetMaxPriceForUpcomingDeparturesAsync();

            return new SeferAramaContextViewModel
            {
                Istasyonlar = await _searchService.GetAllStationsAsync(),
                AramaKriterleri = new SearchQueryDto
                {
                    TravelDate = DateTime.Today,
                    PassengerCount = 1
                },
                Sonuclar = upcomingDepartures,
                MaxPrice = maxPrice
            };
        }

        public async Task<ServiceResult<SeferAramaContextViewModel>> AraAsync(SearchQueryDto searchQuery)
        {
            if (searchQuery.OriginStationId <= 0 || searchQuery.DestinationStationId <= 0)
            {
                return ServiceResult<SeferAramaContextViewModel>.Fail(
                    ServiceResultType.ValidationError,
                    "Please select origin and destination stations.");
            }

            if (searchQuery.OriginStationId == searchQuery.DestinationStationId)
            {
                return ServiceResult<SeferAramaContextViewModel>.Fail(
                    ServiceResultType.ValidationError,
                    "Origin and destination stations cannot be the same.");
            }

            if (searchQuery.TravelDate.Date < DateTime.Today)
            {
                return ServiceResult<SeferAramaContextViewModel>.Fail(
                    ServiceResultType.ValidationError,
                    "Past dates cannot be selected.");
            }

            if (searchQuery.PassengerCount <= 0)
                searchQuery.PassengerCount = 1;

            var departures = await _searchService.SearchDeparturesAsync(searchQuery);
            var allStations = await _searchService.GetAllStationsAsync();
            var maxPrice = await _searchService.GetMaxPriceForUpcomingDeparturesAsync();

            var context = new SeferAramaContextViewModel
            {
                AramaKriterleri = searchQuery,
                Istasyonlar = allStations,
                Sonuclar = departures,
                KalkisIstasyonu = allStations.FirstOrDefault(s => s.Id == searchQuery.OriginStationId),
                VarisIstasyonu = allStations.FirstOrDefault(s => s.Id == searchQuery.DestinationStationId),
                MaxPrice = maxPrice
            };

            return ServiceResult<SeferAramaContextViewModel>.Ok(context);
        }

        public async Task<ServiceResult<SeferDetayViewModel>> GetDetayAsync(int seferId)
        {
            if (seferId <= 0)
                return ServiceResult<SeferDetayViewModel>.Fail(ServiceResultType.NotFound, "Departure not found.");

            var departure = await _searchService.GetDepartureByIdAsync(seferId);
            if (departure == null)
                return ServiceResult<SeferDetayViewModel>.Fail(ServiceResultType.NotFound, "Departure not found.");

            if (departure.DepartureTime <= DateTime.UtcNow)
            {
                return ServiceResult<SeferDetayViewModel>.Fail(
                    ServiceResultType.Conflict,
                    "Ticket sales for this departure have ended.");
            }

            var seats = await _searchService.GetSeatsForDepartureAsync(seferId);
            var viewModel = SeferDetayViewModel.Create(departure, seats.ToList());
            return ServiceResult<SeferDetayViewModel>.Ok(viewModel);
        }

        public async Task<ServiceResult<IEnumerable<SeatInfoDto>>> GetKoltukDurumuAsync(int seferId)
        {
            if (seferId <= 0)
                return ServiceResult<IEnumerable<SeatInfoDto>>.Fail(ServiceResultType.ValidationError, "Invalid departure ID.");

            var seats = await _searchService.GetSeatsForDepartureAsync(seferId);
            return ServiceResult<IEnumerable<SeatInfoDto>>.Ok(seats);
        }

        public async Task<ServiceResult<IEnumerable<SeatInfoDto>>> GetMusaitKoltuklarAsync(int seferId)
        {
            if (seferId <= 0)
                return ServiceResult<IEnumerable<SeatInfoDto>>.Fail(ServiceResultType.ValidationError, "Invalid departure ID.");

            var seats = await _searchService.GetAvailableSeatsForDepartureAsync(seferId);
            return ServiceResult<IEnumerable<SeatInfoDto>>.Ok(seats);
        }

        public Task<IEnumerable<StationInfoDto>> TumIstasyonlarAsync()
        {
            return _searchService.GetAllStationsAsync();
        }

        public async Task<IEnumerable<StationInfoDto>> IstasyonAraAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<StationInfoDto>();

            var normalizedQuery = query.Trim();
            if (normalizedQuery.Length < 2 || normalizedQuery.Length > AppConfig.MaxStationSearchQueryLength)
                return Enumerable.Empty<StationInfoDto>();

            if (!normalizedQuery.All(c =>
                    char.IsLetterOrDigit(c) ||
                    char.IsWhiteSpace(c) ||
                    c == '-' || c == '\'' || c == '.'))
            {
                return Enumerable.Empty<StationInfoDto>();
            }

            return await _searchService.SearchStationsAsync(normalizedQuery);
        }

        public Task<IEnumerable<DepartureResponseDto>> GetYaklasanSeferlerAsync(int count = 10)
        {
            if (count <= 0) count = AppConfig.DefaultUpcomingDepartureCount;
            if (count > AppConfig.MaxUpcomingDepartureCount) count = AppConfig.MaxUpcomingDepartureCount;

            return _searchService.GetUpcomingDeparturesAsync(count);
        }
    }
}
