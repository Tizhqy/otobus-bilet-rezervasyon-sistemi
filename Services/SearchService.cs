using OtobusBiletRezervasyon.DTOs.Search;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class SearchService : ISearchService
    {
        private readonly IDepartureRepository _departureRepository;
        private readonly ISeatRepository _seatRepository;

        public SearchService(IDepartureRepository departureRepository, ISeatRepository seatRepository)
        {
            _departureRepository = departureRepository;
            _seatRepository = seatRepository;
        }

        public async Task<IEnumerable<DepartureResponseDto>> SearchDeparturesAsync(SearchQueryDto searchQuery)
        {
            var departures = await _departureRepository.SearchAsync(
                searchQuery.OriginStationId,
                searchQuery.DestinationStationId,
                searchQuery.TravelDate
            );

            var result = new List<DepartureResponseDto>();

            foreach (var departure in departures)
            {
                var availableSeats = await _seatRepository.GetAvailableCountAsync(departure.Id);
                var totalSeats = await _seatRepository.GetTotalCountAsync(departure.Id);

                result.Add(MapToDepartureResponseDto(departure, availableSeats, totalSeats));
            }

            return result;
        }

        public async Task<DepartureResponseDto?> GetDepartureByIdAsync(int departureId)
        {
            var departure = await _departureRepository.GetByIdWithDetailsAsync(departureId);
            if (departure == null) return null;

            var availableSeats = await _seatRepository.GetAvailableCountAsync(departureId);
            var totalSeats = await _seatRepository.GetTotalCountAsync(departureId);

            return MapToDepartureResponseDto(departure, availableSeats, totalSeats);
        }

        public async Task<IEnumerable<DepartureResponseDto>> GetUpcomingDeparturesAsync(int count = 10)
        {
            var departures = await _departureRepository.GetUpcomingAsync(count);

            var result = new List<DepartureResponseDto>();

            foreach (var departure in departures)
            {
                var availableSeats = await _seatRepository.GetAvailableCountAsync(departure.Id);
                var totalSeats = await _seatRepository.GetTotalCountAsync(departure.Id);

                result.Add(MapToDepartureResponseDto(departure, availableSeats, totalSeats));
            }

            return result;
        }

        /// <summary>
        /// Finds the maximum price among ALL upcoming departures.
        /// Used for scaling the price slider correctly on the frontend.
        /// (Includes all departures, not just the visible ones)
        /// </summary>
        public async Task<decimal> GetMaxPriceForUpcomingDeparturesAsync()
        {
            var baseMaxPrice = await _departureRepository.GetMaxPriceAsync();
            
            // Margin for high demand pricing (%10 surcharge)
            var withSurgeMargin = baseMaxPrice * 1.10m;
            
            // Extra margin for UI (%5)
            return Math.Ceiling(withSurgeMargin * 1.05m);
        }

        public async Task<IEnumerable<StationInfoDto>> GetAllStationsAsync()
        {
            var stations = await _departureRepository.GetActiveStationsAsync();
            return stations.Select(MapToStationInfoDto);
        }

        public async Task<IEnumerable<StationInfoDto>> SearchStationsAsync(string query)
        {
            var stations = await _departureRepository.GetActiveStationsAsync();

            return stations
                .Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || s.City.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(MapToStationInfoDto);
        }

        public async Task<IEnumerable<SeatInfoDto>> GetSeatsForDepartureAsync(int departureId)
        {
            var seats = await _seatRepository.GetByDepartureIdAsync(departureId);
            return seats.Select(MapToSeatInfoDto);
        }

        public async Task<IEnumerable<SeatInfoDto>> GetAvailableSeatsForDepartureAsync(int departureId)
        {
            var seats = await _seatRepository.GetAvailableByDepartureIdAsync(departureId);
            return seats.Select(MapToSeatInfoDto);
        }

        private static DepartureResponseDto MapToDepartureResponseDto(Departure departure, int availableSeats, int totalSeats)
        {
            var route = departure.Route;
            var bus = departure.Bus;
            var originStation = MapRouteStation(route?.OriginStation, route?.OriginStationId ?? 0, "Departure Station");
            var destinationStation = MapRouteStation(route?.DestinationStation, route?.DestinationStationId ?? 0, "Arrival Station");

            bool applyDynamicPricing = availableSeats <= 10 && availableSeats > 0;
            decimal finalPrice = applyDynamicPricing ? departure.Price * 1.10m : departure.Price;

            return new DepartureResponseDto
            {
                Id = departure.Id,
                Route = new RouteInfoDto
                {
                    Id = route?.Id ?? departure.RouteId,
                    OriginStation = originStation,
                    DestinationStation = destinationStation,
                    DistanceKm = route?.DistanceKm,
                    DurationMinutes = route?.DurationMinutes
                },
                Bus = new BusInfoDto
                {
                    Id = bus?.Id ?? departure.BusId,
                    PlateNumber = NormalizeDisplayValue(bus?.PlateNumber, $"Bus #{departure.BusId}"),
                    Capacity = bus?.Capacity ?? 0,
                    Type = NormalizeDisplayValue(bus?.Type, "Standard")
                },
                DepartureTime = departure.DepartureTime,
                ArrivalTime = departure.ArrivalTime,
                Price = finalPrice,
                AvailableSeats = availableSeats,
                TotalSeats = totalSeats,
                IsDynamicPricingApplied = applyDynamicPricing
            };
        }

        private static StationInfoDto MapRouteStation(Station? station, int fallbackId, string fallbackName)
        {
            var name = NormalizeDisplayValue(station?.Name, fallbackName);
            var city = NormalizeDisplayValue(station?.City, name);

            return new StationInfoDto
            {
                Id = station?.Id ?? fallbackId,
                Name = name,
                City = city
            };
        }

        private static string NormalizeDisplayValue(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static StationInfoDto MapToStationInfoDto(Station station)
        {
            var name = NormalizeDisplayValue(station.Name, $"Station #{station.Id}");
            return new StationInfoDto
            {
                Id = station.Id,
                Name = name,
                City = NormalizeDisplayValue(station.City, name)
            };
        }

        private static SeatInfoDto MapToSeatInfoDto(Seat seat)
        {
            return new SeatInfoDto
            {
                Id = seat.Id,
                SeatNumber = seat.SeatNumber,
                Status = seat.Status.ToString()
            };
        }
    }
}
