using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using DaycareAPI.Models;

namespace DaycareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TestController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "API is working!", timestamp = DateTime.UtcNow });
        }

        [HttpPost("test-login")]
        public IActionResult TestLogin()
        {
            return Ok(new { 
                message = "Login endpoint is reachable", 
                timestamp = DateTime.UtcNow,
                corsEnabled = true
            });
        }

        [HttpGet("check-admin")]
        public async Task<IActionResult> CheckAdmin()
        {
            var adminUser = await _userManager.FindByEmailAsync("admin@daycare.com");
            return Ok(new {
                adminExists = adminUser != null,
                adminEmail = adminUser?.Email,
                adminId = adminUser?.Id
            });
        }
    }
}