namespace DaycareAPI.DTOs
{
    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? ProfilePicture { get; set; }
    }
}