using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    [Table("stations")]
    public class Station
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("city")]
        public string City { get; set; } = string.Empty;

        [Column("address")]
        public string? Address { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Route> OriginRoutes { get; set; } = new List<Route>();
        public virtual ICollection<Route> DestinationRoutes { get; set; } = new List<Route>();
        public virtual ICollection<RouteStation> RouteStations { get; set; } = new List<RouteStation>();
    }
}
