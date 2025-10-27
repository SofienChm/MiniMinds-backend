using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaycareAPI.Models
{
    public class DailyActivity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChildId { get; set; }

        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; } = string.Empty; // Nap, Eat, Play, Diaper Change, etc.

        [Required]
        public DateTime ActivityTime { get; set; }

        public TimeSpan? Duration { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? FoodItem { get; set; } // For eating activities

        [StringLength(50)]
        public string? Mood { get; set; } // Happy, Sad, Cranky, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("ChildId")]
        public Child? Child { get; set; }
    }
}