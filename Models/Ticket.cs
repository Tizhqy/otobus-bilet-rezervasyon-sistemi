using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    public enum TicketStatus
    {
        Pending,
        Confirmed,
        Cancelled
    }

    [Table("tickets")]
    public class Ticket
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("departure_id")]
        public int DepartureId { get; set; }

        [Required]
        [Column("total_price", TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        [Column("status")]
        public TicketStatus Status { get; set; } = TicketStatus.Pending;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("DepartureId")]
        public virtual Departure Departure { get; set; } = null!;

        public virtual ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();
        public virtual Payment? Payment { get; set; }
    }
}
