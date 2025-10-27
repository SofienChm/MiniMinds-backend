using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.DTOs
{
    public class ProgramDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public int Capacity { get; set; }
        
        [Required]
        public int MinAge { get; set; }
        
        [Required]
        public int MaxAge { get; set; }
        
        [Required]
        public string Date { get; set; } = string.Empty;
        
        [Required]
        public string StartTime { get; set; } = string.Empty;
        
        [Required]
        public string EndTime { get; set; } = string.Empty;
    }
}