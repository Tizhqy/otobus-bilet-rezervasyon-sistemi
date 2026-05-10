using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    [Table("route_stations")]
    public class RouteStation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("route_id")]
        public int RouteId { get; set; }

        [Required]
        [Column("station_id")]
        public int StationId { get; set; }

        [Required]
        [Column("stop_order")]
        public int StopOrder { get; set; }

        // Navigation properties
        [ForeignKey("RouteId")]
        public virtual Route Route { get; set; } = null!;

        [ForeignKey("StationId")]
        public virtual Station Station { get; set; } = null!;
    }
}
