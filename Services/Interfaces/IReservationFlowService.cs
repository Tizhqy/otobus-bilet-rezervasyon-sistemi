using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    /// <summary>
    /// Reservation Flow Service
    /// Koltuk Seçimi → Temporary Reservation → Ödeme → Bilet Onayı
    /// arasındaki tüm iş mantığını yönetir (FlowService prensibi).
    /// </summary>
    public interface IReservationFlowService
    {
        /// <summary>
        /// Aşama 1: Koltuk seçiminden sonra geçici rezervasyon oluştur
        /// </summary>
        Task<(bool Success, string Message, int? ReservationId)> CreateTemporaryReservationAsync(
            int userId,
            int departureId,
            List<int> seatIds,
            List<(string Name, string Surname, string TCNo)> passengerDetails,
            string? couponCode = null);

        /// <summary>
        /// Aşama 2: Geçici rezervasyonu doğrula ve bilete dönüştür (ödeme sonrası)
        /// </summary>
        Task<(bool Success, string Message, int? TicketId)> ConvertReservationToTicketAsync(
            int reservationId,
            string idempotencyKey);

        /// <summary>
        /// Aşama 3: Süresi dolmuş rezervasyonları otomatik temizle
        /// </summary>
        Task<int> CleanupExpiredReservationsAsync();

        /// <summary>
        /// Geçici rezervasyonu iptal et (kullanıcı vazgeçerse)
        /// </summary>
        Task<bool> CancelReservationAsync(int reservationId);

        /// <summary>
        /// Geçici rezervasyonun durumunu kontrol et
        /// </summary>
        Task<TemporaryReservation?> GetReservationAsync(int reservationId);
    }
}
