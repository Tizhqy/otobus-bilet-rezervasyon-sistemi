using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Repositories.Interfaces
{
    public interface ITemporaryReservationRepository
    {
        /// <summary>
        /// Yeni geçici rezervasyon oluştur
        /// </summary>
        Task<TemporaryReservation> CreateAsync(TemporaryReservation reservation);

        /// <summary>
        /// ID'ye göre geçici rezervasyon getir
        /// </summary>
        Task<TemporaryReservation?> GetByIdAsync(int id);

        /// <summary>
        /// Idempotency Key ile getir (duplicate payment'i engelleme)
        /// </summary>
        Task<TemporaryReservation?> GetByIdempotencyKeyAsync(string idempotencyKey);

        /// <summary>
        /// Kullanıcının en son aktif rezervasyonunu getir
        /// </summary>
        Task<TemporaryReservation?> GetActiveByUserAndDepartureAsync(int userId, int departureId);

        /// <summary>
        /// Süresi dolmuş tüm rezervasyonları getir (cleanup için)
        /// </summary>
        Task<IEnumerable<TemporaryReservation>> GetExpiredAsync();

        /// <summary>
        /// Rezervasyonu güncelle (status değişikliği vb.)
        /// </summary>
        Task<TemporaryReservation> UpdateAsync(TemporaryReservation reservation);

        /// <summary>
        /// Rezervasyonu sil
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Süresi dolmuş tüm rezervasyonları sil (cleanup job'u için)
        /// </summary>
        Task DeleteExpiredAsync();
    }
}
