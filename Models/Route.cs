using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    [Table("routes")]
    public class Route
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("origin_station_id")]
        public int OriginStationId { get; set; }

        [Required]
        [Column("destination_station_id")]
        public int DestinationStationId { get; set; }

        [Column("distance_km")]
        public int? DistanceKm { get; set; }

        [Column("duration_minutes")]
        public int? DurationMinutes { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("OriginStationId")]
        public virtual Station OriginStation { get; set; } = null!;

        [ForeignKey("DestinationStationId")]
        public virtual Station DestinationStation { get; set; } = null!;

        public virtual ICollection<RouteStation> RouteStations { get; set; } = new List<RouteStation>();
        public virtual ICollection<Departure> Departures { get; set; } = new List<Departure>();
    }
}
