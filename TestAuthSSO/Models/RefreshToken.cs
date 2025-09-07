using System.ComponentModel.DataAnnotations;

namespace TestAuthSSO.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; set; } = false;
        public string? RevokedReason { get; set; }
        
        // Foreign key
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        // For token rotation
        public string? ReplacedByToken { get; set; }
    }
}
