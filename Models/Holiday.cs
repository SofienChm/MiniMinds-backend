using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.Models
{
    public class Holiday
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public bool IsRecurring { get; set; } = false;
        
        [StringLength(20)]
        public string? RecurrenceType { get; set; } // "yearly", "monthly", etc.
        
        [StringLength(7)]
        public string Color { get; set; } = "#FF6B6B"; // Default red color
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}