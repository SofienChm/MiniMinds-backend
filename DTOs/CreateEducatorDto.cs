using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.DTOs
{
    public class CreateEducatorDto
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Address { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public DateTime HireDate { get; set; }

        public string? Specialization { get; set; }

        [Required]
        public decimal Salary { get; set; }

        public string? ProfilePicture { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
    }
}