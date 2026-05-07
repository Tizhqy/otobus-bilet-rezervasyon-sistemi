using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services.BackgroundJobs
{
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationCleanupService> _logger;

        public ReservationCleanupService(IServiceScopeFactory scopeFactory, ILogger<ReservationCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReservationCleanupService basladi.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var reservationFlowService = scope.ServiceProvider.GetRequiredService<IReservationFlowService>();
                    
                    int cleanedCount = await reservationFlowService.CleanupExpiredReservationsAsync();
                    
                    if (cleanedCount > 0)
                    {
                        _logger.LogInformation("{Count} adet suresi dolan rezervasyon temizlendi.", cleanedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ReservationCleanupService calisirken hata olustu.");
                }

                // Her 1 dakikada bir kontrol et
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("ReservationCleanupService durduruldu.");
        }
    }
}
