using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Repositories.Interfaces
{
    public interface ISeatRepository
    {
        Task<Seat?> GetByIdAsync(int id);
        Task<IEnumerable<Seat>> GetByDepartureIdAsync(int departureId);
        Task<IEnumerable<Seat>> GetAvailableByDepartureIdAsync(int departureId);
        Task<Seat?> GetByDepartureAndSeatNumberAsync(int departureId, string seatNumber);
        Task<Seat> CreateAsync(Seat seat);
        Task<Seat> UpdateAsync(Seat seat);
        Task<bool> UpdateStatusAsync(int id, SeatStatus status);
        Task<bool> BookSeatAsync(int seatId, int departureId);
        Task<bool> ReleaseSeatAsync(int seatId);
        Task<int> GetAvailableCountAsync(int departureId);
        Task<int> GetTotalCountAsync(int departureId);
        Task CreateSeatsForDepartureAsync(int departureId, int capacity);
    }
}
