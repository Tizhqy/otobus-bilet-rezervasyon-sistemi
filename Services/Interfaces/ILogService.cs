using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface ILogService
    {
        // Logging
        Task LogAsync(int? userId, string action, string? description = null, string? ipAddress = null);
        Task LogLoginAsync(int userId, string ipAddress);
        Task LogLogoutAsync(int userId, string ipAddress);
        Task LogRegistrationAsync(int userId, string ipAddress);
        Task LogTicketPurchaseAsync(int userId, int ticketId, string ipAddress);
        Task LogTicketCancellationAsync(int userId, int ticketId, string ipAddress);
        Task LogPasswordResetRequestAsync(int userId, string ipAddress);
        Task LogPasswordChangeAsync(int userId, string ipAddress);
        Task LogAdminActionAsync(int adminUserId, string action, string description, string ipAddress);

        // Query
        Task<IEnumerable<Log>> GetLogsAsync();
        Task<IEnumerable<Log>> GetLogsByUserIdAsync(int userId);
        Task<IEnumerable<Log>> GetLogsByActionAsync(string action);
        Task<IEnumerable<Log>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Log>> GetRecentLogsAsync(int count = 100);

        // Cleanup
        Task<bool> DeleteOldLogsAsync(int daysOld);
    }
}
