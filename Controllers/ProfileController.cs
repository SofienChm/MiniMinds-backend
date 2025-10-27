using Microsoft.AspNetCore.Mvc;
using DaycareAPI.DTOs;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public ProfileController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
        [HttpGet]
        public IActionResult GetProfile()
        {
            // Return current user profile data
            var profile = new
            {
                firstName = "Admin",
                lastName = "User",
                email = "admin@daycare.com",
                city = "New York",
                profilePicture = (string?)null,
                role = "Admin"
            };
            
            return Ok(profile);
        }

        [HttpPut]
        public IActionResult UpdateProfile([FromBody] UpdateProfileDto model)
        {
            // Validate input
            if (string.IsNullOrEmpty(model.FirstName) || string.IsNullOrEmpty(model.LastName) || string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new { message = "First name, last name, and email are required" });
            }

            // In a real app, update the user in database
            // For now, just return success
            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { message = "Only image files are allowed" });

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
                return BadRequest(new { message = "File size must be less than 5MB" });

            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"/uploads/profiles/{fileName}";
            return Ok(new { imageUrl });
        }

        [HttpPut("change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordDto model)
        {
            // Validate input
            if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest(new { message = "Current password and new password are required" });
            }

            // In a real app, verify current password and update
            // For now, just return success
            return Ok(new { message = "Password changed successfully" });
        }
    }
}