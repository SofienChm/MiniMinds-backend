using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        [Required]
        public string SenderId { get; set; } = string.Empty;
        
        [Required]
        public string RecipientId { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        public bool IsRead { get; set; } = false;
        
        // Navigation properties
        public ApplicationUser? Sender { get; set; }
        public ApplicationUser? Recipient { get; set; }
    }
}