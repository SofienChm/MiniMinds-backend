using DaycareAPI.Data;
using DaycareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SeedController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Manually trigger database seeding
        /// </summary>
        /// <returns>Seeding result</returns>
        [HttpPost("run")]
        public async Task<IActionResult> RunSeeder()
        {
            try
            {
                var seeder = new DatabaseSeeder(_context, _userManager, _roleManager);
                await seeder.SeedAsync();

                return Ok(new
                {
                    success = true,
                    message = "Database seeded successfully!",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error seeding database",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Clear all data from the database (USE WITH CAUTION!)
        /// </summary>
        /// <returns>Clear result</returns>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearDatabase()
        {
            try
            {
                // Remove all data
                _context.Notifications.RemoveRange(_context.Notifications);
                _context.DailyActivities.RemoveRange(_context.DailyActivities);
                _context.Attendances.RemoveRange(_context.Attendances);
                _context.Children.RemoveRange(_context.Children);
                _context.Parents.RemoveRange(_context.Parents);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Database cleared successfully!",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error clearing database",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Reset database - Clear and reseed
        /// </summary>
        /// <returns>Reset result</returns>
        [HttpPost("reset")]
        public async Task<IActionResult> ResetDatabase()
        {
            try
            {
                // Clear existing data
                _context.Notifications.RemoveRange(_context.Notifications);
                _context.DailyActivities.RemoveRange(_context.DailyActivities);
                _context.Attendances.RemoveRange(_context.Attendances);
                _context.Children.RemoveRange(_context.Children);
                _context.Parents.RemoveRange(_context.Parents);

                await _context.SaveChangesAsync();

                // Reseed
                var seeder = new DatabaseSeeder(_context, _userManager, _roleManager);
                await seeder.SeedAsync();

                return Ok(new
                {
                    success = true,
                    message = "Database reset and reseeded successfully!",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error resetting database",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get seeding status
        /// </summary>
        /// <returns>Database statistics</returns>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var stats = new
            {
                parents = _context.Parents.Count(),
                children = _context.Children.Count(),
                attendances = _context.Attendances.Count(),
                dailyActivities = _context.DailyActivities.Count(),
                notifications = _context.Notifications.Count(),
                timestamp = DateTime.Now
            };

            return Ok(stats);
        }
    }
}