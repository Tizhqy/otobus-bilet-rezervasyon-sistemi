using System.ComponentModel.DataAnnotations;

namespace OtobusBiletRezervasyon.DTOs.Admin
{
    public class AdminCouponDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kupon kodu zorunludur.")]
        [StringLength(20, ErrorMessage = "Kupon kodu en fazla 20 karakter olabilir.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indirim miktari/yuzdesi zorunludur.")]
        [Range(1, 999999, ErrorMessage = "Gecerli bir indirim tutari giriniz.")]
        public decimal DiscountAmount { get; set; }

        [Required(ErrorMessage = "Indirim tipi zorunludur.")]
        public string DiscountType { get; set; } = "Percentage";

        [Required(ErrorMessage = "Son kullanim tarihi zorunludur.")]
        public DateTime? ValidUntil { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
