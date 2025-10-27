using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.Models
{
    public class TeacherChild
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeacherId { get; set; }

        [Required]
        public int ChildId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Teacher? Teacher { get; set; }
        public Child? Child { get; set; }
    }
}
