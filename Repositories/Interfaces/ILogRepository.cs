using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Repositories.Interfaces
{
    public interface ILogRepository
    {
        Task<Log?> GetByIdAsync(int id);
        Task<IEnumerable<Log>> GetAllAsync();
        Task<IEnumerable<Log>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Log>> GetByActionAsync(string action);
        Task<IEnumerable<Log>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Log>> GetRecentAsync(int count = 100);
        Task<Log> CreateAsync(Log log);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteOlderThanAsync(DateTime date);
    }
}
