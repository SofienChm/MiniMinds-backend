using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.Models
{
    public class DaycareProgram
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public int Capacity { get; set; }
        
        [Required]
        public int MinAge { get; set; }
        
        [Required]
        public int MaxAge { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public TimeSpan StartTime { get; set; }
        
        [Required]
        public TimeSpan EndTime { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<ProgramEnrollment> Enrollments { get; set; } = new List<ProgramEnrollment>();
    }
}