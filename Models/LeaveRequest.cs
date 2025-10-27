using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaycareAPI.Models
{
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public Teacher? Teacher { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // Computed and stored for simplicity
        [Required]
        public int Days { get; set; }

        [StringLength(1000)]
        public string? Reason { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        // Admin who approved/rejected
        public string? ApprovedByUserId { get; set; }
        public ApplicationUser? ApprovedByUser { get; set; }
    }
}