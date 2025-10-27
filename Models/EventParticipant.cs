using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.Models
{
    public class EventParticipant
    {
        public int Id { get; set; }
        
        [Required]
        public int EventId { get; set; }
        public Event? Event { get; set; }
        
        [Required]
        public int ChildId { get; set; }
        public Child? Child { get; set; }
        
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        
        public string RegisteredBy { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string Status { get; set; } = "Registered";
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}