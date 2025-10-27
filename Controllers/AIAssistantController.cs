using DaycareAPI.Data;
using DaycareAPI.Models;
using DaycareAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;
using System.Security.Claims;
using System.Linq;

namespace DaycareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public partial class AIAssistantController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly OpenAIService _openAIService;
        private readonly ILogger<AIAssistantController> _logger;

        public AIAssistantController(ApplicationDbContext context, OpenAIService openAIService, ILogger<AIAssistantController> logger)
        {
            _context = context;
            _openAIService = openAIService;
            _logger = logger;
        }

        [HttpPost("query")]
        public async Task<IActionResult> ProcessQuery([FromBody] AIQueryRequest request)
        {
            try
            {
                var response = await ProcessNaturalLanguageQuery(request.Query);
                return Ok(new { success = true, response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI query: {Query}", request.Query);
                return BadRequest(new { success = false, message = "Sorry, I couldn't process your request. Please try rephrasing." });
            }
        }

        private async Task<AIResponse> ProcessNaturalLanguageQuery(string query)
        {
            // Get relevant data based on query context
            var contextData = await GatherContextData(query);
            
            // Use OpenAI to generate intelligent response
            var aiResponse = await _openAIService.GenerateResponseAsync(query, contextData);
            
            // Also get structured data for display
            var structuredData = await GetStructuredData(query);
            
            return new AIResponse
            {
                Message = aiResponse,
                Data = structuredData
            };
        }
        
        private async Task<string> GatherContextData(string query)
        {
            var lowerQuery = query.ToLower();
            var contextParts = new List<string>();
            
            // Meals: include child/date-specific data when present
            if (ContainsKeywords(lowerQuery, ["eat", "food", "meal", "lunch", "breakfast", "snack"]))
            {
                var date = ExtractDate(query);
                var childName = ExtractChildName(query);
                var restrictToParent = IsParent() && (lowerQuery.Contains("my child") || string.IsNullOrEmpty(childName));
                var meals = await GetMeals(childName, date, restrictToParent);
                contextParts.Add($"Meals: {JsonSerializer.Serialize(meals)}");
            }
            
            if (ContainsKeywords(lowerQuery, ["attendance", "present", "absent", "here"]))
            {
                var attendance = await GetRecentAttendance();
                contextParts.Add($"Recent Attendance: {JsonSerializer.Serialize(attendance)}");
            }
            
            if (ContainsKeywords(lowerQuery, ["activity", "activities", "nap", "play", "sleep"]))
            {
                var activities = await GetRecentActivities();
                contextParts.Add($"Recent Activities: {JsonSerializer.Serialize(activities)}");
            }
            
            if (ContainsKeywords(lowerQuery, ["child", "children", "kid", "kids"]))
            {
                var children = await GetChildrenInfo();
                contextParts.Add($"Children Info: {JsonSerializer.Serialize(children)}");
            }
            
            if (ContainsKeywords(lowerQuery, ["event", "events"]))
            {
                var events = await GetEventsInfo();
                contextParts.Add($"Events: {JsonSerializer.Serialize(events)}");
            }
            
            return string.Join("\n\n", contextParts);
        }
        
        private async Task<object?> GetStructuredData(string query)
        {
            var lowerQuery = query.ToLower();
            
            if (ContainsKeywords(lowerQuery, ["eat", "food", "meal", "lunch", "breakfast", "snack"]))
            {
                var date = ExtractDate(query);
                var childName = ExtractChildName(query);
                var restrictToParent = IsParent() && (lowerQuery.Contains("my child") || string.IsNullOrEmpty(childName));
                return await GetMeals(childName, date, restrictToParent);
            }
            if (ContainsKeywords(lowerQuery, ["attendance", "present", "absent"]))
                return await GetRecentAttendance();
            if (ContainsKeywords(lowerQuery, ["activity", "activities", "nap", "play"]))
                return await GetRecentActivities();
            if (ContainsKeywords(lowerQuery, ["child", "children"]))
                return await GetChildrenInfo();
            if (ContainsKeywords(lowerQuery, ["event", "events"]))
                return await GetEventsInfo();
                
            return null;
        }

        private async Task<object> GetMeals(string? childName, DateTime? date, bool restrictToParent)
        {
            var query = _context.DailyActivities
                .Include(a => a.Child)
                .Where(a => a.ActivityType.ToLower() == "eat" && !string.IsNullOrEmpty(a.FoodItem))
                .AsQueryable();

            if (date.HasValue)
            {
                query = query.Where(a => a.ActivityTime.Date == date.Value.Date);
            }
            else
            {
                // default to last 7 days if no specific date
                query = query.Where(a => a.ActivityTime.Date >= DateTime.Today.AddDays(-7));
            }

            if (restrictToParent)
            {
                var parentId = GetParentId();
                if (parentId.HasValue)
                {
                    var childIds = await _context.Children
                        .Where(c => c.ChildParents.Any(cp => cp.ParentId == parentId.Value))
                        .Select(c => c.Id)
                        .ToListAsync();
                    if (childIds.Any())
                    {
                        query = query.Where(a => childIds.Contains(a.ChildId));
                    }
                }
            }

            if (!string.IsNullOrEmpty(childName))
            {
                var nameLower = childName.ToLower();
                query = query.Where(a => a.Child!.FirstName.ToLower().Contains(nameLower) || a.Child!.LastName.ToLower().Contains(nameLower));
            }

            return await query
                .OrderBy(a => a.ActivityTime)
                .Select(a => new
                {
                    ChildName = $"{a.Child!.FirstName} {a.Child.LastName}",
                    FoodItem = a.FoodItem,
                    Time = a.ActivityTime,
                    Notes = a.Notes
                })
                .ToListAsync();
        }

        private bool IsParent()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return string.Equals(role, "Parent", StringComparison.OrdinalIgnoreCase);
        }

        private int? GetParentId()
        {
            var parentIdClaim = User.FindFirst("ParentId")?.Value;
            if (int.TryParse(parentIdClaim, out var parentId)) return parentId;
            return null;
        }

        private DateTime? ExtractDate(string query)
        {
            // Today
            if (query.Contains("today"))
                return DateTime.Today;
            
            // Yesterday
            if (query.Contains("yesterday"))
                return DateTime.Today.AddDays(-1);
            
            // Tomorrow
            if (query.Contains("tomorrow"))
                return DateTime.Today.AddDays(1);

            // This week (return Monday of this week)
            if (query.Contains("this week"))
                return DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

            // Enhanced explicit date parsing
            var formats = new[]
            {
                "dd/MM/yyyy","d/M/yyyy","MM/dd/yyyy","M/d/yyyy",
                "yyyy-MM-dd","yyyy/MM/dd",
                "dd MMM yyyy","d MMM yyyy","MMM dd, yyyy","MMMM dd, yyyy"
            };

            var patterns = new[]
            {
                @"\b\d{4}[\/\-]\d{1,2}[\/\-]\d{1,2}\b", // yyyy-MM-dd or yyyy/MM/dd
                @"\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{4}\b", // dd/MM/yyyy or MM/dd/yyyy
                @"\b\d{1,2}\s+[A-Za-z]{3,}\s+\d{4}\b",     // 20 Oct 2025
                @"\b[A-Za-z]{3,}\s+\d{1,2},\s+\d{4}\b"     // Oct 20, 2025
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(query, pattern);
                if (match.Success)
                {
                    var token = match.Value;
                    foreach (var fmt in formats)
                    {
                        if (DateTime.TryParseExact(token, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        {
                            return dt.Date;
                        }
                    }
                    if (DateTime.TryParse(token, out var parsed))
                    {
                        return parsed.Date;
                    }
                }
            }

            return null;
        }

        private string? ExtractChildName(string query)
        {
            // Simple name extraction - look for common patterns
            var namePatterns = new[]
            {
                @"for (\w+)",
                @"child (\w+)",
                @"kid (\w+)",
                @"(\w+)'s"
            };

            foreach (var pattern in namePatterns)
            {
                var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        private string? ExtractActivityType(string query)
        {
            var activityKeywords = new Dictionary<string, string>
            {
                { "nap", "nap" },
                { "sleep", "nap" },
                { "play", "play" },
                { "diaper", "diaper change" },
                { "eat", "eat" },
                { "meal", "eat" }
            };

            foreach (var keyword in activityKeywords)
            {
                if (query.Contains(keyword.Key))
                {
                    return keyword.Value;
                }
            }

            return null;
        }

        [HttpPost("daily-summary")]
        public async Task<IActionResult> GenerateDailySummary([FromBody] DailySummaryRequest request)
        {
            try
            {
                var summary = await GenerateDailySummaryForChild(request.ChildId, request.Date);
                return Ok(new { success = true, summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily summary for child {ChildId}", request.ChildId);
                return BadRequest(new { success = false, message = "Could not generate daily summary." });
            }
        }

        private async Task<string> GenerateDailySummaryForChild(int childId, DateTime date)
        {
            var child = await _context.Children.FindAsync(childId);
            if (child == null)
            {
                return "Child not found.";
            }

            var activities = await _context.DailyActivities
                .Where(a => a.ChildId == childId && a.ActivityTime.Date == date.Date)
                .OrderBy(a => a.ActivityTime)
                .ToListAsync();

            var attendance = await _context.Attendances
                .Where(a => a.ChildId == childId && a.Date.Date == date.Date)
                .FirstOrDefaultAsync();

            var contextData = new StringBuilder();
            contextData.AppendLine($"Summary for {child.FirstName} {child.LastName} on {date.ToShortDateString()}:");

            if (attendance != null)
            {
                contextData.AppendLine($"- Arrived at {attendance.CheckInTime:t}.");
                if (attendance.CheckOutTime.HasValue)
                {
                    contextData.AppendLine($"- Departed at {attendance.CheckOutTime.Value:t}.");
                }
            }
            else
            {
                contextData.AppendLine("- Child was absent today.");
            }

            var meals = activities.Where(a => a.ActivityType.ToLower() == "eat").ToList();
            if (meals.Any())
            {
                contextData.AppendLine("\nMeals:");
                foreach (var meal in meals)
                {
                    contextData.AppendLine($"- Ate {meal.FoodItem} at {meal.ActivityTime:t}. Notes: {meal.Notes}");
                }
            }

            var naps = activities.Where(a => a.ActivityType.ToLower() == "nap").ToList();
            if (naps.Any())
            {
                contextData.AppendLine("\nNaps:");
                foreach (var nap in naps)
                {
                    contextData.AppendLine($"- Napped from {nap.ActivityTime:t} for {nap.Duration} minutes. Mood: {nap.Mood}. Notes: {nap.Notes}");
                }
            }

            var playActivities = activities.Where(a => a.ActivityType.ToLower() == "play").ToList();
            if (playActivities.Any())
            {
                contextData.AppendLine("\nPlaytime:");
                foreach (var activity in playActivities)
                {
                    contextData.AppendLine($"- Played at {activity.ActivityTime:t}. Notes: {activity.Notes}");
                }
            }

            var diaperChanges = activities.Where(a => a.ActivityType.ToLower() == "diaper change").ToList();
            if (diaperChanges.Any())
            {
                contextData.AppendLine("\nDiaper Changes:");
                foreach (var change in diaperChanges)
                {
                    contextData.AppendLine($"- Diaper change at {change.ActivityTime:t}. Notes: {change.Notes}");
                }
            }

            if (!activities.Any())
            {
                return $"No activities found for {child.FirstName} on {date.ToShortDateString()}.";
            }

            var userQuery = "Please provide a friendly, narrative summary of the child\'s day for their parents based on these activities. Start with a greeting, like \'Here is a summary of [Child\'s Name]\'s day\'.";

            var aiResponse = await _openAIService.GenerateResponseAsync(userQuery, contextData.ToString());

            return aiResponse;
        }
    }

    public class AIQueryRequest
    {
        public string Query { get; set; } = string.Empty;
    }

    public class DailySummaryRequest
    {
        public int ChildId { get; set; }
        public DateTime Date { get; set; }
    }

    public class AIResponse
    {
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}



// Add back helper methods used elsewhere
namespace DaycareAPI.Controllers
{
    public partial class AIAssistantController
    {
        private bool ContainsKeywords(string query, string[] keywords)
        {
            return keywords.Any(keyword => query.Contains(keyword));
        }

        private async Task<object> GetRecentAttendance()
        {
            return await _context.Attendances
                .Include(a => a.Child)
                .Where(a => a.Date >= DateTime.Today.AddDays(-7))
                .OrderByDescending(a => a.Date)
                .Select(a => new
                {
                    ChildName = $"{a.Child!.FirstName} {a.Child.LastName}",
                    Date = a.Date,
                    CheckInTime = a.CheckInTime,
                    CheckOutTime = a.CheckOutTime,
                    Status = a.CheckOutTime.HasValue ? "Present" : "Checked In"
                })
                .Take(20)
                .ToListAsync();
        }

        private async Task<object> GetRecentActivities()
        {
            return await _context.DailyActivities
                .Include(a => a.Child)
                .Where(a => a.ActivityTime >= DateTime.Today.AddDays(-7))
                .OrderByDescending(a => a.ActivityTime)
                .Select(a => new
                {
                    ChildName = $"{a.Child!.FirstName} {a.Child.LastName}",
                    ActivityType = a.ActivityType,
                    Time = a.ActivityTime,
                    Duration = a.Duration,
                    Notes = a.Notes,
                    Mood = a.Mood
                })
                .Take(20)
                .ToListAsync();
        }

        private async Task<object> GetChildrenInfo()
        {
            return await _context.Children
                .Include(c => c.Parent)
                .Select(c => new
                {
                    Name = $"{c.FirstName} {c.LastName}",
                    Age = DateTime.Now.Year - c.DateOfBirth.Year,
                    Gender = c.Gender,
                    ParentName = $"{c.Parent!.FirstName} {c.Parent.LastName}",
                    EnrollmentDate = c.EnrollmentDate,
                    Allergies = c.Allergies,
                    MedicalNotes = c.MedicalNotes
                })
                .ToListAsync();
        }

        private async Task<object> GetEventsInfo()
        {
            return await _context.Events
                .OrderBy(e => e.CreatedAt)
                .Select(e => new
                {
                    Name = e.Name,
                    Type = e.Type,
                    Description = e.Description,
                    Price = e.Price,
                    AgeRange = $"{e.AgeFrom}-{e.AgeTo} years",
                    Capacity = e.Capacity,
                    Time = e.Time
                })
                .ToListAsync();
        }
    }
}