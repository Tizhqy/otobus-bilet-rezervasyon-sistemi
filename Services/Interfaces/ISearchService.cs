using OtobusBiletRezervasyon.DTOs.Search;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface ISearchService
    {
        // Departure Search
        Task<IEnumerable<DepartureResponseDto>> SearchDeparturesAsync(SearchQueryDto searchQuery);
        Task<DepartureResponseDto?> GetDepartureByIdAsync(int departureId);
        Task<IEnumerable<DepartureResponseDto>> GetUpcomingDeparturesAsync(int count = 10);

        // Station Search
        Task<IEnumerable<StationInfoDto>> GetAllStationsAsync();
        Task<IEnumerable<StationInfoDto>> SearchStationsAsync(string query);

        // Seat Info
        Task<IEnumerable<SeatInfoDto>> GetSeatsForDepartureAsync(int departureId);
        Task<IEnumerable<SeatInfoDto>> GetAvailableSeatsForDepartureAsync(int departureId);
    }
}
