using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;

namespace OtobusBiletRezervasyon.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly AppDbContext _context;

        public TicketRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Ticket?> GetByIdAsync(int id)
        {
            return await _context.Tickets.FindAsync(id);
        }

        public async Task<Ticket?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Departure)
                    .ThenInclude(d => d.Route)
                        .ThenInclude(r => r.OriginStation)
                .Include(t => t.Departure)
                    .ThenInclude(d => d.Route)
                        .ThenInclude(r => r.DestinationStation)
                .Include(t => t.Departure)
                    .ThenInclude(d => d.Bus)
                .Include(t => t.Passengers)
                    .ThenInclude(p => p.Seat)
                .Include(t => t.Payment)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Ticket>> GetByUserIdAsync(int userId)
        {
            return await _context.Tickets
                .Include(t => t.Departure)
                    .ThenInclude(d => d.Route)
                        .ThenInclude(r => r.OriginStation)
                .Include(t => t.Departure)
                    .ThenInclude(d => d.Route)
                        .ThenInclude(r => r.DestinationStation)
                .Include(t => t.Departure)
                    .ThenInclude(d => d.Bus)
                .Include(t => t.Passengers)
                    .ThenInclude(p => p.Seat)
                .Include(t => t.Payment)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetByDepartureIdAsync(int departureId)
        {
            return await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Passengers)
                    .ThenInclude(p => p.Seat)
                .Where(t => t.DepartureId == departureId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetAllAsync()
        {
            return await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Departure)
                    .ThenInclude(d => d.Route)
                        .ThenInclude(r => r.OriginStation)
                .Include(t => t.Departure)
                    .ThenInclude(d => d.Route)
                        .ThenInclude(r => r.DestinationStation)
                .Include(t => t.Departure)
                    .ThenInclude(d => d.Bus)
                .Include(t => t.Passengers)
                    .ThenInclude(p => p.Seat)
                .Include(t => t.Payment)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Ticket> CreateAsync(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public async Task<Ticket> UpdateAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return false;

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateStatusAsync(int id, TicketStatus status)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                ticket.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        // Passenger
        public async Task<Passenger> CreatePassengerAsync(Passenger passenger)
        {
            _context.Passengers.Add(passenger);
            await _context.SaveChangesAsync();
            return passenger;
        }

        public async Task<IEnumerable<Passenger>> GetPassengersByTicketIdAsync(int ticketId)
        {
            return await _context.Passengers
                .Include(p => p.Seat)
                .Where(p => p.TicketId == ticketId)
                .ToListAsync();
        }

        // Payment
        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> UpdatePaymentAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment?> GetPaymentByTicketIdAsync(int ticketId)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.TicketId == ticketId);
        }

        public async Task UpdatePaymentStatusAsync(int paymentId, PaymentStatus status)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                payment.Status = status;
                if (status == PaymentStatus.Completed)
                {
                    payment.PaidAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
