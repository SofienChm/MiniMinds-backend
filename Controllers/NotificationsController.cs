using DaycareAPI.Data;
using DaycareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Notifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] bool includeRead = false)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = GetUserIdByRole();
            
            Console.WriteLine($"*** Getting notifications for role: {userRole}, userId: {userId} (includeRead: {includeRead})");
            
            var query = _context.Notifications.AsQueryable();
            
            Console.WriteLine($"*** Filtering notifications - Role: {userRole}, UserId: {userId} ***");
            
            // Filter by user role
            if (userRole == "Admin")
            {
                // Admin sees all notifications (no filter)
                Console.WriteLine("*** Admin - showing all notifications ***");
            }
            else if (userRole == "Teacher")
            {
                // Teacher sees notifications without UserId or their specific ones
                query = query.Where(n => string.IsNullOrEmpty(n.UserId) || n.UserId == userId);
                Console.WriteLine($"*** Teacher - filtering by UserId: {userId} or null ***");
            }
            else if (userRole == "Parent")
            {
                // Parents see only their notifications
                query = query.Where(n => n.UserId == userId);
                Console.WriteLine($"*** Parent - filtering by UserId: {userId} ***");
            }
            else
            {
                Console.WriteLine("*** Unknown role - returning empty ***");
                return Ok(new List<Notification>());
            }
            
            if (!includeRead)
            {
                query = query.Where(n => !n.IsRead);
            }
            
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
                
            Console.WriteLine($"*** Found {notifications.Count} notifications for {userRole}");
            
            return notifications;
        }

        // GET: api/Notifications/All
        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetAllNotifications()
        {
            Console.WriteLine("*** Getting all notifications");
            return await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // GET: api/Notifications/Unread
        [HttpGet("Unread")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUnreadNotifications()
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = GetUserIdByRole();
                
                Console.WriteLine($"Getting unread notifications for role: {userRole}, userId: {userId}");
                
                if (string.IsNullOrEmpty(userRole))
                {
                    return Ok(new List<Notification>());
                }
                
                var query = _context.Notifications.Where(n => !n.IsRead);
                
                if (userRole == "Admin")
                {
                    // Admin sees all
                    Console.WriteLine("*** Admin - all unread ***");
                }
                else if (userRole == "Teacher")
                {
                    query = query.Where(n => string.IsNullOrEmpty(n.UserId) || n.UserId == userId);
                }
                else if (userRole == "Parent")
                {
                    query = query.Where(n => n.UserId == userId);
                }
                
                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
                    
                Console.WriteLine($"Returning {notifications.Count} unread notifications");
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread notifications: {ex.Message}");
                return Ok(new List<Notification>());
            }
        }

        // GET: api/Notifications/Count
        [HttpGet("Count")]
        public async Task<ActionResult<object>> GetUnreadCount()
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = GetUserIdByRole();
                
                Console.WriteLine($"Getting notification count for role: {userRole}, userId: {userId}");
                
                var query = _context.Notifications.Where(n => !n.IsRead);
                
                if (userRole == "Admin")
                {
                    // Admin sees all
                    Console.WriteLine("*** Admin - counting all unread ***");
                }
                else if (userRole == "Teacher")
                {
                    query = query.Where(n => string.IsNullOrEmpty(n.UserId) || n.UserId == userId);
                }
                else if (userRole == "Parent")
                {
                    query = query.Where(n => n.UserId == userId);
                }
                else
                {
                    return new { count = 0 };
                }
                
                var count = await query.CountAsync();
                Console.WriteLine($"Found {count} unread notifications for {userRole}");
                    
                return new { count };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notification count: {ex.Message}");
                return new { count = 0 };
            }
        }

        // PUT: api/Notifications/5/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Notifications/MarkAsRead/5
        [HttpPost("MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsReadPost(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/Notifications/MarkAllAsRead
        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var notifications = await _context.Notifications
                .Where(n => !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Notifications/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        private string? GetUserIdByRole()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userRole == "Parent")
            {
                return User.FindFirst("ParentId")?.Value;
            }
            else if (userRole == "Teacher")
            {
                return User.FindFirst("TeacherId")?.Value;
            }
            
            return null;
        }

        // POST: api/Notifications/CheckReminders (Manual trigger for testing)
        [HttpPost("CheckReminders")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CheckReminders()
        {
            try
            {
                await CheckBirthdayReminders();
                await CheckFeeReminders();
                return Ok(new { message = "Reminders checked and notifications created" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task CheckBirthdayReminders()
        {
            var today = DateTime.UtcNow.Date;
            var in3Days = today.AddDays(3);

            var upcomingBirthdays = await _context.Children
                .Include(c => c.Parent)
                .Where(c => c.IsActive)
                .ToListAsync();

            var birthdayChildren = upcomingBirthdays.Where(c =>
            {
                var birthday = new DateTime(today.Year, c.DateOfBirth.Month, c.DateOfBirth.Day);
                return birthday >= today && birthday <= in3Days;
            }).ToList();

            foreach (var child in birthdayChildren)
            {
                var birthday = new DateTime(today.Year, child.DateOfBirth.Month, child.DateOfBirth.Day);
                var daysUntil = (birthday - today).Days;
                var age = today.Year - child.DateOfBirth.Year;

                var message = daysUntil == 0 
                    ? $"🎂 Today is {child.FirstName} {child.LastName}'s birthday! They are turning {age} years old."
                    : $"🎂 {child.FirstName} {child.LastName}'s birthday is in {daysUntil} day(s). They will be {age} years old.";

                var notification = new Notification
                {
                    Type = "Birthday",
                    Title = "Birthday Reminder",
                    Message = message,
                    RedirectUrl = $"/children/detail/{child.Id}",
                    UserId = string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        private async Task CheckFeeReminders()
        {
            var today = DateTime.UtcNow.Date;
            var in7Days = today.AddDays(7);

            var upcomingFees = await _context.Fees
                .Include(f => f.Child)
                    .ThenInclude(c => c.Parent)
                .Where(f => f.Status == "pending" && f.DueDate.Date >= today && f.DueDate.Date <= in7Days)
                .ToListAsync();

            foreach (var fee in upcomingFees)
            {
                var daysUntil = (fee.DueDate.Date - today).Days;

                if (daysUntil <= 7)
                {
                    var urgency = daysUntil <= 1 ? "⚠️ URGENT: " : "";
                    var message = $"{urgency}Payment reminder: ${fee.Amount} for {fee.Child!.FirstName} {fee.Child.LastName} is due in {daysUntil} day(s). {fee.Description}";

                    var notification = new Notification
                    {
                        Type = "FeeReminder",
                        Title = "Payment Reminder",
                        Message = message,
                        RedirectUrl = "/fees",
                        UserId = fee.Child.ParentId.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}