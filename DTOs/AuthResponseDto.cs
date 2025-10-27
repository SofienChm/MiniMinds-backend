namespace DaycareAPI.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string PreferredLanguage { get; set; } = "en";
        public string Role { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}