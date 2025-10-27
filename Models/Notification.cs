using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaycareAPI.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        public string Type { get; set; } = string.Empty;
        
        public string Title { get; set; } = string.Empty;
        
        public string Message { get; set; } = string.Empty;
        
        public string? RedirectUrl { get; set; }
        
        public string? UserId { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}