using System.ComponentModel.DataAnnotations;

namespace GuardianCapitalLLC.Models
{
    public class UserSession
    {
        [Key]
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public string? IPAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime? LastAccessedAt { get; set; }
    }
}
