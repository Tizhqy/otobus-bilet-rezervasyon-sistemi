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
                searchQuery.Date
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
            return new DepartureResponseDto
            {
                Id = departure.Id,
                Route = new RouteInfoDto
                {
                    Id = departure.Route.Id,
                    OriginStation = new StationInfoDto
                    {
                        Id = departure.Route.OriginStation.Id,
                        Name = departure.Route.OriginStation.Name,
                        City = departure.Route.OriginStation.City
                    },
                    DestinationStation = new StationInfoDto
                    {
                        Id = departure.Route.DestinationStation.Id,
                        Name = departure.Route.DestinationStation.Name,
                        City = departure.Route.DestinationStation.City
                    },
                    DistanceKm = departure.Route.DistanceKm,
                    DurationMinutes = departure.Route.DurationMinutes
                },
                Bus = new BusInfoDto
                {
                    Id = departure.Bus.Id,
                    PlateNumber = departure.Bus.PlateNumber,
                    Capacity = departure.Bus.Capacity,
                    Type = departure.Bus.Type
                },
                DepartureTime = departure.DepartureTime,
                ArrivalTime = departure.ArrivalTime,
                Price = departure.Price,
                AvailableSeats = availableSeats,
                TotalSeats = totalSeats
            };
        }

        private static StationInfoDto MapToStationInfoDto(Station station)
        {
            return new StationInfoDto
            {
                Id = station.Id,
                Name = station.Name,
                City = station.City
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
