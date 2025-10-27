using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.Models
{
    public class Event
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public int AgeFrom { get; set; }
        
        [Required]
        public int AgeTo { get; set; }
        
        [Required]
        public int Capacity { get; set; }
        
        [Required]
        public string Time { get; set; } = string.Empty;
        
        public string? Image { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}