using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    [Table("coupons")]
    public class Coupon
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column("discount_amount", TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// "Percentage" or "Fixed"
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Column("discount_type")]
        public string DiscountType { get; set; } = "Percentage";

        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
