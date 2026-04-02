using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    [Table("departures")]
    public class Departure
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("route_id")]
        public int RouteId { get; set; }

        [Required]
        [Column("bus_id")]
        public int BusId { get; set; }

        [Required]
        [Column("departure_time")]
        public DateTime DepartureTime { get; set; }

        [Required]
        [Column("arrival_time")]
        public DateTime ArrivalTime { get; set; }

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("RouteId")]
        public virtual Route Route { get; set; } = null!;

        [ForeignKey("BusId")]
        public virtual Bus Bus { get; set; } = null!;

        public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
