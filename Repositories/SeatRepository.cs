using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;

namespace OtobusBiletRezervasyon.Repositories
{
    public class SeatRepository : ISeatRepository
    {
        private readonly AppDbContext _context;

        public SeatRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Seat?> GetByIdAsync(int id)
        {
            return await _context.Seats.FindAsync(id);
        }

        public async Task<IEnumerable<Seat>> GetByDepartureIdAsync(int departureId)
        {
            return await _context.Seats
                .Where(s => s.DepartureId == departureId)
                .OrderBy(s => s.SeatNumber.Length)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Seat>> GetAvailableByDepartureIdAsync(int departureId)
        {
            return await _context.Seats
                .Where(s => s.DepartureId == departureId && s.Status == SeatStatus.Available)
                .OrderBy(s => s.SeatNumber.Length)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();
        }

        public async Task<Seat?> GetByDepartureAndSeatNumberAsync(int departureId, string seatNumber)
        {
            return await _context.Seats
                .FirstOrDefaultAsync(s => s.DepartureId == departureId && s.SeatNumber == seatNumber);
        }

        public async Task<Seat> CreateAsync(Seat seat)
        {
            _context.Seats.Add(seat);
            await _context.SaveChangesAsync();
            return seat;
        }

        public async Task<Seat> UpdateAsync(Seat seat)
        {
            _context.Seats.Update(seat);
            await _context.SaveChangesAsync();
            return seat;
        }

        public async Task<bool> UpdateStatusAsync(int id, SeatStatus status)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat == null) return false;

            seat.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BookSeatAsync(int seatId, int departureId)
        {
            var seat = await _context.Seats
                .FirstOrDefaultAsync(s => s.Id == seatId
                    && s.DepartureId == departureId
                    && s.Status == SeatStatus.Available);

            if (seat == null) return false;

            seat.Status = SeatStatus.Booked;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReleaseSeatAsync(int seatId)
        {
            var seat = await _context.Seats.FindAsync(seatId);
            if (seat == null) return false;

            seat.Status = SeatStatus.Available;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AreSeatsAvailableAsync(int departureId, IEnumerable<int> seatIds)
        {
            var normalizedSeatIds = seatIds
                .Where(seatId => seatId > 0)
                .Distinct()
                .ToList();

            if (!normalizedSeatIds.Any())
                return true;

            var availableSeatCount = await _context.Seats
                .AsNoTracking()
                .CountAsync(s =>
                    s.DepartureId == departureId &&
                    s.Status == SeatStatus.Available &&
                    normalizedSeatIds.Contains(s.Id));

            return availableSeatCount == normalizedSeatIds.Count;
        }

        public async Task<int> GetAvailableCountAsync(int departureId)
        {
            return await _context.Seats
                .CountAsync(s => s.DepartureId == departureId && s.Status == SeatStatus.Available);
        }

        public async Task<int> GetTotalCountAsync(int departureId)
        {
            return await _context.Seats
                .CountAsync(s => s.DepartureId == departureId);
        }

        public async Task CreateSeatsForDepartureAsync(int departureId, int capacity)
        {
            var seats = new List<Seat>();
            for (int i = 1; i <= capacity; i++)
            {
                seats.Add(new Seat
                {
                    DepartureId = departureId,
                    SeatNumber = i.ToString(),
                    Status = SeatStatus.Available
                });
            }

            await _context.Seats.AddRangeAsync(seats);
            await _context.SaveChangesAsync();
        }
    }
}
