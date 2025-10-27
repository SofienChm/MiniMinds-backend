using DaycareAPI.Data;
using DaycareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Claims;

namespace DaycareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BasicAIController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BasicAIController> _logger;

        public BasicAIController(ApplicationDbContext context, ILogger<BasicAIController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("query")]
        public async Task<IActionResult> ProcessQuery([FromBody] BasicAIQueryRequest request)
        {
            try
            {
                var response = await ProcessPatternMatchingQuery(request.Query);
                return Ok(new { success = true, response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing basic AI query: {Query}", request.Query);
                return BadRequest(new { success = false, message = "Sorry, I couldn't process your request. Please try rephrasing." });
            }
        }

        private async Task<BasicAIResponse> ProcessPatternMatchingQuery(string query)
        {
            var lowerQuery = query.ToLower();
            
            if (ContainsKeywords(lowerQuery, ["eat", "food", "meal", "lunch", "breakfast", "snack"]))
            {
                return await HandleFoodQuery(lowerQuery);
            }
            else if (ContainsKeywords(lowerQuery, ["attendance", "present", "absent", "here"]))
            {
                return await HandleAttendanceQuery(lowerQuery);
            }
            else if (ContainsKeywords(lowerQuery, ["activity", "activities", "nap", "play", "sleep"]))
            {
                return await HandleActivityQuery(lowerQuery);
            }
            else if (ContainsKeywords(lowerQuery, ["child", "children", "kid", "kids"]))
            {
                return await HandleChildrenQuery(lowerQuery);
            }
            else if (ContainsKeywords(lowerQuery, ["event", "events"]))
            {
                return await HandleEventQuery(lowerQuery);
            }
            else if (ContainsKeywords(lowerQuery, ["allergy", "allergies", "medical"]))
            {
                return await HandleAllergyQuery(lowerQuery);
            }
            else if (ContainsKeywords(lowerQuery, ["parent", "parents", "contact"]))
            {
                return await HandleParentQuery(lowerQuery);
            }
            else if (ContainsKeywords(lowerQuery, ["age", "birthday", "birth"]))
            {
                return await HandleAgeQuery(lowerQuery);
            }
            
            return new BasicAIResponse
            {
                Message = "[Pattern Matching] I can help with: meals, attendance, activities, events, allergies, parents, ages. Try asking 'Which children have allergies?' or 'Show me parent contacts'",
                Data = null
            };
        }

        private async Task<BasicAIResponse> HandleFoodQuery(string query)
        {
            var lowerQuery = query.ToLower();
            var date = ExtractDate(query);
            var childName = ExtractChildName(query);
            var mentionsMyChild = lowerQuery.Contains("my child") || lowerQuery.Contains("my kid") || lowerQuery.Contains("my baby");

            var activitiesQuery = _context.DailyActivities
                .Include(a => a.Child)
                .Where(a => a.ActivityType.ToLower() == "eat" && !string.IsNullOrEmpty(a.FoodItem))
                .AsQueryable();

            if (date.HasValue)
            {
                activitiesQuery = activitiesQuery.Where(a => a.ActivityTime.Date == date.Value.Date);
            }

            if (IsParent() && (mentionsMyChild || string.IsNullOrEmpty(childName)))
            {
                var parentId = GetParentId();
                if (parentId.HasValue)
                {
                    var parentChildren = await _context.Children
                        .Where(c => c.ChildParents.Any(cp => cp.ParentId == parentId.Value))
                        .ToListAsync();

                    if (parentChildren.Count == 0)
                    {
                        return new BasicAIResponse { Message = "[Pattern Matching] I couldn't find any children linked to your account.", Data = null };
                    }

                    if (string.IsNullOrEmpty(childName))
                    {
                        if (parentChildren.Count == 1)
                        {
                            var onlyChildId = parentChildren[0].Id;
                            activitiesQuery = activitiesQuery.Where(a => a.ChildId == onlyChildId);
                        }
                        else
                        {
                            var names = string.Join(", ", parentChildren.Select(c => $"{c.FirstName} {c.LastName}"));
                            return new BasicAIResponse
                            {
                                Message = $"[Pattern Matching] You have {parentChildren.Count} children: {names}. Please specify which child (e.g., 'for Emma').",
                                Data = null
                            };
                        }
                    }
                    else
                    {
                        var matchingIds = parentChildren
                            .Where(c => c.FirstName.ToLower().Contains(childName) || c.LastName.ToLower().Contains(childName))
                            .Select(c => c.Id)
                            .ToList();

                        if (!matchingIds.Any())
                        {
                            return new BasicAIResponse { Message = "[Pattern Matching] I couldn't find that child in your account. Please check the name.", Data = null };
                        }

                        activitiesQuery = activitiesQuery.Where(a => matchingIds.Contains(a.ChildId));
                    }
                }
            }
            else if (!string.IsNullOrEmpty(childName))
            {
                activitiesQuery = activitiesQuery.Where(a =>
                    a.Child!.FirstName.ToLower().Contains(childName) ||
                    a.Child!.LastName.ToLower().Contains(childName));
            }

            var activities = await activitiesQuery
                .OrderBy(a => a.ActivityTime)
                .Select(a => new
                {
                    ChildName = $"{a.Child!.FirstName} {a.Child.LastName}",
                    FoodItem = a.FoodItem,
                    Time = a.ActivityTime,
                    Notes = a.Notes
                })
                .ToListAsync();

            var message = activities.Any()
                ? $"[Pattern Matching] Found {activities.Count} meal record(s)" + (date.HasValue ? $" for {date.Value:MMM dd, yyyy}" : "")
                : "[Pattern Matching] No meal records found for your query.";

            return new BasicAIResponse { Message = message, Data = activities };
        }

        private async Task<BasicAIResponse> HandleAttendanceQuery(string query)
        {
            var date = ExtractDate(query);
            
            var attendanceQuery = _context.Attendances
                .Include(a => a.Child)
                .AsQueryable();

            if (date.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.Date.Date == date.Value.Date);
            }

            var attendances = await attendanceQuery
                .OrderBy(a => a.Date)
                .Select(a => new
                {
                    ChildName = $"{a.Child!.FirstName} {a.Child.LastName}",
                    Date = a.Date,
                    CheckInTime = a.CheckInTime,
                    CheckOutTime = a.CheckOutTime,
                    Status = a.CheckOutTime.HasValue ? "Present" : "Checked In"
                })
                .ToListAsync();

            var message = attendances.Any() 
                ? $"[Pattern Matching] Found {attendances.Count} attendance records" + (date.HasValue ? $" for {date.Value:MMM dd, yyyy}" : "")
                : "[Pattern Matching] No attendance records found for your query.";

            return new BasicAIResponse { Message = message, Data = attendances };
        }

        private async Task<BasicAIResponse> HandleActivityQuery(string query)
        {
            var activities = await _context.DailyActivities
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

            var message = activities.Any() 
                ? $"[Pattern Matching] Found {activities.Count} recent activity records"
                : "[Pattern Matching] No activity records found.";

            return new BasicAIResponse { Message = message, Data = activities };
        }

        private async Task<BasicAIResponse> HandleChildrenQuery(string query)
        {
            var children = await _context.Children
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

            return new BasicAIResponse 
            { 
                Message = $"[Pattern Matching] Found {children.Count} children in the system", 
                Data = children 
            };
        }

        private async Task<BasicAIResponse> HandleEventQuery(string query)
        {
            var events = await _context.Events
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

            var message = events.Any() 
                ? $"[Pattern Matching] Found {events.Count} events"
                : "[Pattern Matching] No events found.";

            return new BasicAIResponse { Message = message, Data = events };
        }

        private async Task<BasicAIResponse> HandleAllergyQuery(string query)
        {
            var children = await _context.Children
                .Include(c => c.Parent)
                .Where(c => !string.IsNullOrEmpty(c.Allergies) || !string.IsNullOrEmpty(c.MedicalNotes))
                .Select(c => new
                {
                    Name = $"{c.FirstName} {c.LastName}",
                    Age = DateTime.Now.Year - c.DateOfBirth.Year,
                    Allergies = c.Allergies,
                    MedicalNotes = c.MedicalNotes,
                    ParentName = $"{c.Parent!.FirstName} {c.Parent.LastName}"
                })
                .ToListAsync();

            var message = children.Any() 
                ? $"[Pattern Matching] Found {children.Count} children with allergies or medical notes"
                : "[Pattern Matching] No children with allergies or medical notes found.";

            return new BasicAIResponse { Message = message, Data = children };
        }

        private async Task<BasicAIResponse> HandleParentQuery(string query)
        {
            var parents = await _context.Parents
                .Include(p => p.Children)
                .Select(p => new
                {
                    Name = $"{p.FirstName} {p.LastName}",
                    Email = p.Email,
                    Phone = p.PhoneNumber,
                    Address = p.Address,
                    EmergencyContact = p.EmergencyContact,
                    Children = p.Children.Select(c => $"{c.FirstName} {c.LastName}").ToList()
                })
                .ToListAsync();

            var message = parents.Any() 
                ? $"[Pattern Matching] Found {parents.Count} parent contacts"
                : "[Pattern Matching] No parent contacts found.";

            return new BasicAIResponse { Message = message, Data = parents };
        }

        private async Task<BasicAIResponse> HandleAgeQuery(string query)
        {
            var children = await _context.Children
                .Select(c => new
                {
                    Name = $"{c.FirstName} {c.LastName}",
                    Age = DateTime.Now.Year - c.DateOfBirth.Year,
                    DateOfBirth = c.DateOfBirth,
                    Gender = c.Gender
                })
                .OrderBy(c => c.Age)
                .ToListAsync();

            var message = children.Any() 
                ? $"[Pattern Matching] Found {children.Count} children with age information"
                : "[Pattern Matching] No children found.";

            return new BasicAIResponse { Message = message, Data = children };
        }

        private bool ContainsKeywords(string query, string[] keywords)
        {
            return keywords.Any(keyword => query.Contains(keyword));
        }

        private DateTime? ExtractDate(string query)
        {
            var lower = query.ToLower();

            if (lower.Contains("today"))
                return DateTime.Today;
            if (lower.Contains("yesterday"))
                return DateTime.Today.AddDays(-1);
            if (lower.Contains("tomorrow"))
                return DateTime.Today.AddDays(1);

            // Try common explicit date formats
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
                    // Try exact formats first
                    foreach (var fmt in formats)
                    {
                        if (DateTime.TryParseExact(token, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        {
                            return dt.Date;
                        }
                    }
                    // Fallback general parse
                    if (DateTime.TryParse(token, out var parsed))
                    {
                        return parsed.Date;
                    }
                }
            }

            return null;
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

        private string? ExtractChildName(string query)
        {
            var namePatterns = new[]
            {
                @"for (\w+)",
                @"child (\w+)",
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
    }

    public class BasicAIQueryRequest
    {
        public string Query { get; set; } = string.Empty;
    }

    public class BasicAIResponse
    {
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}