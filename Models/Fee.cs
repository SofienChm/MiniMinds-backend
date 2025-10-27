using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaycareAPI.Models
{
    public class Fee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChildId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? PaidDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "pending"; // pending, paid, overdue

        [Required]
        [StringLength(20)]
        public string FeeType { get; set; } = "monthly"; // monthly, one-time, late-fee

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ChildId")]
        public Child? Child { get; set; }
    }
}