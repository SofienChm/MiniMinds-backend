using DaycareAPI.DTOs;
using DaycareAPI.Models;
using DaycareAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Assign role
            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleResult.Succeeded)
                return BadRequest(roleResult.Errors);

            // Create corresponding record based on role
            if (model.Role == "Parent")
            {
                var parent = new Parent
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = "", // Will be updated later
                    CreatedAt = DateTime.UtcNow
                };
                _context.Parents.Add(parent);
                await _context.SaveChangesAsync();
            }
            // Note: Teacher records are handled by the TeachersController for now

            return Ok(new { message = "User registered successfully", role = model.Role });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password" });

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "Admin";
            var token = await GenerateJwtToken(user);

            var response = new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture,
                PreferredLanguage = user.PreferredLanguage,
                Role = userRole,
                Expiration = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:ExpiryInHours"]))
            };

            return Ok(response);
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Admin";
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, role)
            };
            
            // Add role-specific claims
            if (role == "Parent")
            {
                var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Email == user.Email);
                if (parent != null)
                {
                    claims.Add(new Claim("ParentId", parent.Id.ToString()));
                }
            }
            else if (role == "Teacher")
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == user.Email);
                if (teacher != null)
                {
                    claims.Add(new Claim("TeacherId", teacher.Id.ToString()));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:ExpiryInHours"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Authorize]
        [HttpPut("update-language")]
        public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            // Fallback to email claim if user lookup by Id fails
            if (user == null)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(email))
                {
                    user = await _userManager.FindByEmailAsync(email);
                }
            }

            if (user == null)
                return NotFound("User not found");

            user.PreferredLanguage = model.Language;
            user.UpdatedAt = DateTime.UtcNow;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Language updated successfully", language = user.PreferredLanguage });
        }
    }
}