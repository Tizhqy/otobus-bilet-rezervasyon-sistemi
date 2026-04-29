using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    public enum SeatStatus
    {
        Available,
        Booked,
        Reserved
    }

    [Table("seats")]
    public class Seat
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("departure_id")]
        public int DepartureId { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("seat_number")]
        public string SeatNumber { get; set; } = string.Empty;

        [Required]
        [Column("status")]
        public SeatStatus Status { get; set; } = SeatStatus.Available;

        // Navigation properties
        [ForeignKey("DepartureId")]
        public virtual Departure Departure { get; set; } = null!;

        public virtual Passenger? Passenger { get; set; }
    }
}
