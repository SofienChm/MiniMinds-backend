using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.DTOs
{
    public class CreateParentDto
    {
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

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? EmergencyContact { get; set; }

        public string? ProfilePicture { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
    }
}