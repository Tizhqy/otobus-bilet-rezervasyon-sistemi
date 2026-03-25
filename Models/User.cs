using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtobusBiletRezervasyon.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("phone")]
        public string? Phone { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [MaxLength(255)]
        [Column("remember_token")]
        public string? RememberToken { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public virtual ICollection<Log> Logs { get; set; } = new List<Log>();
        public virtual ICollection<PasswordReset> PasswordResets { get; set; } = new List<PasswordReset>();
    }
}
