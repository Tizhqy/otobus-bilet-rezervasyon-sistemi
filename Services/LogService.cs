using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class LogService : ILogService
    {
        private readonly ILogRepository _logRepository;

        public LogService(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task LogAsync(int? userId, string action, string? description = null, string? ipAddress = null)
        {
            var log = new Log
            {
                UserId = userId,
                Action = action,
                Description = description,
                IpAddress = ipAddress
            };

            await _logRepository.CreateAsync(log);
        }

        public async Task LogLoginAsync(int userId, string ipAddress)
        {
            await LogAsync(userId, "LOGIN", "User logged in.", ipAddress);
        }

        public async Task LogLogoutAsync(int userId, string ipAddress)
        {
            await LogAsync(userId, "LOGOUT", "User logged out.", ipAddress);
        }

        public async Task LogRegistrationAsync(int userId, string ipAddress)
        {
            await LogAsync(userId, "REGISTER", "New user registered.", ipAddress);
        }

        public async Task LogTicketPurchaseAsync(int userId, int ticketId, string ipAddress)
        {
            await LogAsync(userId, "TICKET_PURCHASE", $"Ticket #{ticketId} purchased.", ipAddress);
        }

        public async Task LogTicketCancellationAsync(int userId, int ticketId, string ipAddress)
        {
            await LogAsync(userId, "TICKET_CANCEL", $"Ticket #{ticketId} cancelled.", ipAddress);
        }

        public async Task LogPasswordResetRequestAsync(int userId, string ipAddress)
        {
            await LogAsync(userId, "PASSWORD_RESET_REQUEST", "Password reset requested.", ipAddress);
        }

        public async Task LogPasswordChangeAsync(int userId, string ipAddress)
        {
            await LogAsync(userId, "PASSWORD_CHANGE", "Password changed.", ipAddress);
        }

        public async Task LogAdminActionAsync(int adminUserId, string action, string description, string ipAddress)
        {
            await LogAsync(adminUserId, $"ADMIN_{action}", description, ipAddress);
        }

        public async Task<IEnumerable<Log>> GetLogsAsync()
        {
            return await _logRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Log>> GetLogsByUserIdAsync(int userId)
        {
            return await _logRepository.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<Log>> GetLogsByActionAsync(string action)
        {
            return await _logRepository.GetByActionAsync(action);
        }

        public async Task<IEnumerable<Log>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _logRepository.GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<Log>> GetRecentLogsAsync(int count = 100)
        {
            return await _logRepository.GetRecentAsync(count);
        }

        public async Task<bool> DeleteOldLogsAsync(int daysOld)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            return await _logRepository.DeleteOlderThanAsync(cutoffDate);
        }
    }
}
