using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        Paypal
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    [Table("payments")]
    public class Payment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("ticket_id")]
        public int TicketId { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column("method")]
        public PaymentMethod Method { get; set; }

        [Required]
        [Column("status")]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(255)]
        [Column("transaction_id")]
        public string? TransactionId { get; set; }

        [Column("paid_at")]
        public DateTime? PaidAt { get; set; }

        // Navigation property
        [ForeignKey("TicketId")]
        public virtual Ticket Ticket { get; set; } = null!;
    }
}
