namespace DaycareAPI.Models
{
    public class TeacherDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime HireDate { get; set; }
        public string? Specialization { get; set; }
        public decimal Salary { get; set; }
        public string? ProfilePicture { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}