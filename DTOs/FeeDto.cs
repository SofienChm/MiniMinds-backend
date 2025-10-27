using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.DTOs
{
    public class CreateFeeDto
    {
        [Required]
        public int ChildId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }

        [StringLength(20)]
        public string FeeType { get; set; } = "monthly";

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateFeeDto
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal? Amount { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class PayFeeDto
    {
        [Required]
        public int FeeId { get; set; }

        public DateTime? PaidDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? PaymentNotes { get; set; }
    }

    public class FeeResponseDto
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FeeType { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int DaysOverdue { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}