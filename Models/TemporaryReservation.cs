using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    /// <summary>
    /// Geçici Rezervasyon (Temporary Reservation)
    /// Kullanıcı koltuk seçip ödeme sayfasına girmeden 15 dakika süreyle
    /// koltukları "reserve" etmek için kullanılır.
    /// 15 dakika içinde ödeme yapılmazsa otomatik silinir (Background Job).
    /// Amaç: Concurrency sorunu, zombi biletler ve double-booking'i engellemek.
    /// </summary>
    public class TemporaryReservation
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Müşteri ID'si (Rezervasyonu yapan kişi)
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Sefer ID'si
        /// </summary>
        public int DepartureId { get; set; }

        /// <summary>
        /// Seçilen koltukların JSON array'i (örn: [12,13,14])
        /// </summary>
        [Required]
        public string SelectedSeatIds { get; set; } = string.Empty;

        /// <summary>
        /// Yolcu bilgileri (JSON: [{"Name":"Ali","Surname":"Yılmaz","TCNo":"12345678901"},...])
        /// </summary>
        [Required]
        public string PassengerDetails { get; set; } = string.Empty;

        /// <summary>
        /// İşlem için unique key (Idempotency - aynı isteği 2 kez göndersen de sorun olmaz)
        /// </summary>
        [Required]
        public string IdempotencyKey { get; set; } = string.Empty;

        /// <summary>
        /// Toplam Tutar (Dinamik fiyat hesaplaması yapıldıktan sonra)
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Kupon kodu (varsa)
        /// </summary>
        public string? CouponCode { get; set; }

        /// <summary>
        /// Oluşturulma zamanı
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Sona erme zamanı (CreatedAt + 15 dakika)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Rezervasyon durumu: "Active" (aktif), "Converted" (bilete dönüştürüldü), "Expired" (süresi doldu)
        /// </summary>
        public string Status { get; set; } = "Active";

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("DepartureId")]
        public virtual Departure? Departure { get; set; }
    }
}
