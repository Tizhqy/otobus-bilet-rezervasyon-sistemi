using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;

namespace OtobusBiletRezervasyon.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly AppDbContext _context;

        public LogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Log?> GetByIdAsync(int id)
        {
            return await _context.Logs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<IEnumerable<Log>> GetAllAsync()
        {
            return await _context.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Log>> GetByUserIdAsync(int userId)
        {
            return await _context.Logs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Log>> GetByActionAsync(string action)
        {
            return await _context.Logs
                .Include(l => l.User)
                .Where(l => l.Action == action)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Log>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Logs
                .Include(l => l.User)
                .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Log>> GetRecentAsync(int count = 100)
        {
            return await _context.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Log> CreateAsync(Log log)
        {
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var log = await _context.Logs.FindAsync(id);
            if (log == null) return false;

            _context.Logs.Remove(log);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOlderThanAsync(DateTime date)
        {
            var oldLogs = await _context.Logs
                .Where(l => l.CreatedAt < date)
                .ToListAsync();

            if (!oldLogs.Any()) return true;

            _context.Logs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
