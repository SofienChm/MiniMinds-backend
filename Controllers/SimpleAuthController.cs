using Microsoft.AspNetCore.Mvc;
using DaycareAPI.DTOs;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SimpleAuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto model)
        {
            // Simple hardcoded login for testing
            if (model.Email == "admin@daycare.com" && model.Password == "Admin@123")
            {
                var response = new AuthResponseDto
                {
                    Token = "fake-jwt-token-for-testing",
                    Email = "admin@daycare.com",
                    FirstName = "Admin",
                    LastName = "User",
                    ProfilePicture = null,
                    Expiration = DateTime.UtcNow.AddHours(24)
                };
                
                return Ok(response);
            }
            
            return Unauthorized(new { message = "Invalid email or password" });
        }
    }
}