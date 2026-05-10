using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    [Table("passengers")]
    public class Passenger
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("ticket_id")]
        public int TicketId { get; set; }

        [Required]
        [Column("seat_id")]
        public int SeatId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("id_number")]
        public string? IdNumber { get; set; }

        // Navigation properties
        [ForeignKey("TicketId")]
        public virtual Ticket Ticket { get; set; } = null!;

        [ForeignKey("SeatId")]
        public virtual Seat Seat { get; set; } = null!;
    }
}
