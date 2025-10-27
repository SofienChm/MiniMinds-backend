using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaycareAPI.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChildId { get; set; }

        [Required]
        public DateTime CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [StringLength(500)]
        public string? CheckInNotes { get; set; }

        [StringLength(500)]
        public string? CheckOutNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey("ChildId")]
        public Child? Child { get; set; }
    }
}