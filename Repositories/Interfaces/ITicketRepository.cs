using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Repositories.Interfaces
{
    public interface ITicketRepository
    {
        Task<Ticket?> GetByIdAsync(int id);
        Task<Ticket?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Ticket>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Ticket>> GetByDepartureIdAsync(int departureId);
        Task<IEnumerable<Ticket>> GetAllAsync();
        Task<Ticket> CreateAsync(Ticket ticket);
        Task<Ticket> UpdateAsync(Ticket ticket);
        Task<bool> DeleteAsync(int id);
        Task UpdateStatusAsync(int id, TicketStatus status);

        // Passenger
        Task<Passenger> CreatePassengerAsync(Passenger passenger);
        Task<IEnumerable<Passenger>> GetPassengersByTicketIdAsync(int ticketId);

        // Payment
        Task<Payment> CreatePaymentAsync(Payment payment);
        Task<Payment> UpdatePaymentAsync(Payment payment);
        Task<Payment?> GetPaymentByTicketIdAsync(int ticketId);
        Task UpdatePaymentStatusAsync(int paymentId, PaymentStatus status);

        // Transaction support
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}
