using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.DTOs
{
    public class CreateHolidayDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public bool IsRecurring { get; set; } = false;
        
        [StringLength(20)]
        public string? RecurrenceType { get; set; }
        
        [StringLength(7)]
        public string Color { get; set; } = "#FF6B6B";
    }

    public class UpdateHolidayDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public bool IsRecurring { get; set; } = false;
        
        [StringLength(20)]
        public string? RecurrenceType { get; set; }
        
        [StringLength(7)]
        public string Color { get; set; } = "#FF6B6B";
    }
}