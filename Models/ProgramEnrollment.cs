using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.Models
{
    public class ProgramEnrollment
    {
        public int Id { get; set; }
        
        [Required]
        public int ProgramId { get; set; }
        
        [Required]
        public int ChildId { get; set; }
        
        public DateTime EnrolledAt { get; set; }
        
        // Navigation properties
        public virtual DaycareProgram Program { get; set; } = null!;
        public virtual Child Child { get; set; } = null!;
    }
}