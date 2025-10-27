using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public DateTime HireDate { get; set; }

        [StringLength(100)]
        public string? Specialization { get; set; }

        [Required]
        public decimal Salary { get; set; }

        public string? ProfilePicture { get; set; }

        public bool IsActive { get; set; } = true;

        // Annual leave allocation for the teacher (in days)
        public int AnnualLeaveDays { get; set; } = 30;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}