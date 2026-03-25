using OtobusBiletRezervasyon.DTOs.Ticket;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface ITicketService
    {
        // Ticket Operations
        Task<TicketResponseDto?> GetTicketByIdAsync(int ticketId);
        Task<IEnumerable<TicketResponseDto>> GetUserTicketsAsync(int userId);
        Task<IEnumerable<TicketResponseDto>> GetAllTicketsAsync();

        // Purchase - Atomic Transaction
        Task<TicketResponseDto> PurchaseTicketAsync(int userId, CreateTicketDto createTicketDto);

        // Cancellation
        Task<bool> CancelTicketAsync(int ticketId, int userId);

        // Seat Availability
        Task<bool> IsSeatAvailableAsync(int departureId, int seatId);
        Task<bool> AreSeatAvailableAsync(int departureId, IEnumerable<int> seatIds);
    }
}
