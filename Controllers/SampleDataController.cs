using DaycareAPI.Data;
using DaycareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DaycareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SampleDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SampleDataController> _logger;

        public SampleDataController(ApplicationDbContext context, ILogger<SampleDataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("create-sample-data")]
        public async Task<IActionResult> CreateSampleData()
        {
            try
            {
                // Check if sample data already exists
                if (await _context.Children.AnyAsync())
                {
                    return Ok(new { message = "Sample data already exists" });
                }

                // Create sample parents
                var parent1 = new Parent
                {
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "john.smith@email.com",
                    PhoneNumber = "555-0101",
                    Address = "123 Main St",
                    EmergencyContact = "Jane Smith - 555-0102"
                };

                var parent2 = new Parent
                {
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Email = "sarah.johnson@email.com",
                    PhoneNumber = "555-0201",
                    Address = "456 Oak Ave",
                    EmergencyContact = "Mike Johnson - 555-0202"
                };

                _context.Parents.AddRange(parent1, parent2);
                await _context.SaveChangesAsync();

                // Create sample children
                var child1 = new Child
                {
                    FirstName = "Emma",
                    LastName = "Smith",
                    DateOfBirth = DateTime.Now.AddYears(-4),
                    Gender = "Female",
                    Allergies = "Peanuts",
                    ParentId = parent1.Id
                };

                var child2 = new Child
                {
                    FirstName = "Liam",
                    LastName = "Smith",
                    DateOfBirth = DateTime.Now.AddYears(-3),
                    Gender = "Male",
                    ParentId = parent1.Id
                };

                var child3 = new Child
                {
                    FirstName = "Olivia",
                    LastName = "Johnson",
                    DateOfBirth = DateTime.Now.AddYears(-5),
                    Gender = "Female",
                    MedicalNotes = "Asthma - has inhaler",
                    ParentId = parent2.Id
                };

                _context.Children.AddRange(child1, child2, child3);
                await _context.SaveChangesAsync();

                // Create sample daily activities
                var today = DateTime.Today;
                var activities = new List<DailyActivity>
                {
                    // Emma's activities
                    new DailyActivity
                    {
                        ChildId = child1.Id,
                        ActivityType = "Eat",
                        ActivityTime = today.AddHours(8),
                        FoodItem = "Oatmeal with berries",
                        Notes = "Ate well, asked for more berries"
                    },
                    new DailyActivity
                    {
                        ChildId = child1.Id,
                        ActivityType = "Play",
                        ActivityTime = today.AddHours(9),
                        Duration = TimeSpan.FromMinutes(45),
                        Notes = "Played with blocks, built a tower"
                    },
                    new DailyActivity
                    {
                        ChildId = child1.Id,
                        ActivityType = "Nap",
                        ActivityTime = today.AddHours(12),
                        Duration = TimeSpan.FromHours(1.5),
                        Notes = "Slept peacefully"
                    },
                    new DailyActivity
                    {
                        ChildId = child1.Id,
                        ActivityType = "Eat",
                        ActivityTime = today.AddHours(14),
                        FoodItem = "Grilled cheese sandwich and apple slices",
                        Notes = "Finished everything"
                    },

                    // Liam's activities
                    new DailyActivity
                    {
                        ChildId = child2.Id,
                        ActivityType = "Eat",
                        ActivityTime = today.AddHours(8).AddMinutes(15),
                        FoodItem = "Scrambled eggs and toast",
                        Notes = "Left some toast"
                    },
                    new DailyActivity
                    {
                        ChildId = child2.Id,
                        ActivityType = "Play",
                        ActivityTime = today.AddHours(10),
                        Duration = TimeSpan.FromMinutes(30),
                        Notes = "Played with toy cars"
                    },
                    new DailyActivity
                    {
                        ChildId = child2.Id,
                        ActivityType = "Eat",
                        ActivityTime = today.AddHours(12).AddMinutes(30),
                        FoodItem = "Chicken nuggets and vegetables",
                        Notes = "Ate all nuggets, some vegetables"
                    },

                    // Olivia's activities
                    new DailyActivity
                    {
                        ChildId = child3.Id,
                        ActivityType = "Eat",
                        ActivityTime = today.AddHours(8).AddMinutes(30),
                        FoodItem = "Yogurt with granola",
                        Notes = "Enjoyed breakfast"
                    },
                    new DailyActivity
                    {
                        ChildId = child3.Id,
                        ActivityType = "Play",
                        ActivityTime = today.AddHours(9).AddMinutes(30),
                        Duration = TimeSpan.FromMinutes(60),
                        Notes = "Art activity - painted a rainbow"
                    },
                    new DailyActivity
                    {
                        ChildId = child3.Id,
                        ActivityType = "Nap",
                        ActivityTime = today.AddHours(13),
                        Duration = TimeSpan.FromHours(2),
                        Notes = "Long nap, woke up refreshed"
                    }
                };

                _context.DailyActivities.AddRange(activities);

                // Create sample attendance records
                var attendances = new List<Attendance>
                {
                    new Attendance
                    {
                        ChildId = child1.Id,
                        Date = today,
                        CheckInTime = today.AddHours(7).AddMinutes(30),
                        CheckOutTime = today.AddHours(17)
                    },
                    new Attendance
                    {
                        ChildId = child2.Id,
                        Date = today,
                        CheckInTime = today.AddHours(7).AddMinutes(45),
                        CheckOutTime = today.AddHours(16).AddMinutes(30)
                    },
                    new Attendance
                    {
                        ChildId = child3.Id,
                        Date = today,
                        CheckInTime = today.AddHours(8),
                        CheckOutTime = null // Still at daycare
                    }
                };

                _context.Attendances.AddRange(attendances);

                // Create sample events
                var events = new List<Event>
                {
                    new Event
                    {
                        Name = "Spring Festival",
                        Type = "Festival",
                        Description = "Annual spring celebration with games and activities",
                        Price = 0,
                        AgeFrom = 2,
                        AgeTo = 6,
                        Capacity = 50,
                        Time = "10:00 AM - 2:00 PM"
                    },
                    new Event
                    {
                        Name = "Parent-Teacher Meeting",
                        Type = "Meeting",
                        Description = "Monthly meeting to discuss children's progress",
                        Price = 0,
                        AgeFrom = 0,
                        AgeTo = 10,
                        Capacity = 30,
                        Time = "6:00 PM - 8:00 PM"
                    }
                };

                _context.Events.AddRange(events);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Sample data created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample data");
                return BadRequest(new { message = "Error creating sample data", error = ex.Message });
            }
        }
    }
}