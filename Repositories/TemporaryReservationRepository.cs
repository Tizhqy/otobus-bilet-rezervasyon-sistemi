using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;

namespace OtobusBiletRezervasyon.Repositories
{
    public class TemporaryReservationRepository : ITemporaryReservationRepository
    {
        private readonly AppDbContext _context;

        public TemporaryReservationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TemporaryReservation> CreateAsync(TemporaryReservation reservation)
        {
            reservation.CreatedAt = DateTime.UtcNow;
            reservation.ExpiresAt = DateTime.UtcNow.AddMinutes(15); // 15 dakika TTL
            reservation.Status = "Active";

            _context.TemporaryReservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<TemporaryReservation?> GetByIdAsync(int id)
        {
            return await _context.TemporaryReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.Status == "Active");
        }

        public async Task<TemporaryReservation?> GetByIdempotencyKeyAsync(string idempotencyKey)
        {
            return await _context.TemporaryReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.IdempotencyKey == idempotencyKey && tr.Status == "Active");
        }

        public async Task<TemporaryReservation?> GetActiveByUserAndDepartureAsync(int userId, int departureId)
        {
            return await _context.TemporaryReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(tr =>
                    tr.UserId == userId &&
                    tr.DepartureId == departureId &&
                    tr.Status == "Active" &&
                    tr.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<IEnumerable<TemporaryReservation>> GetExpiredAsync()
        {
            return await _context.TemporaryReservations
                .AsNoTracking()
                .Where(tr => tr.ExpiresAt <= DateTime.UtcNow && tr.Status == "Active")
                .ToListAsync();
        }

        public async Task<TemporaryReservation> UpdateAsync(TemporaryReservation reservation)
        {
            _context.TemporaryReservations.Update(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task DeleteAsync(int id)
        {
            var reservation = await _context.TemporaryReservations.FindAsync(id);
            if (reservation != null)
            {
                _context.TemporaryReservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteExpiredAsync()
        {
            var expired = await _context.TemporaryReservations
                .Where(tr => tr.ExpiresAt <= DateTime.UtcNow && tr.Status == "Active")
                .ToListAsync();

            if (expired.Any())
            {
                _context.TemporaryReservations.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }
        }
    }
}
