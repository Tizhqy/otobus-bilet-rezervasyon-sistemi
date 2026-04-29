using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    [Table("buses")]
    public class Bus
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("plate_number")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required]
        [Column("capacity")]
        public int Capacity { get; set; }

        [MaxLength(50)]
        [Column("type")]
        public string? Type { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ICollection<Departure> Departures { get; set; } = new List<Departure>();
    }
}
