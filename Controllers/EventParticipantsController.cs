using DaycareAPI.Data;
using DaycareAPI.Models;
using DaycareAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventParticipantsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventParticipantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/EventParticipants/Event/5
        [HttpGet("Event/{eventId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetEventParticipants(int eventId)
        {
            var participants = await (from ep in _context.EventParticipants
                                    join c in _context.Children on ep.ChildId equals c.Id
                                    join cp in _context.ChildParents on c.Id equals cp.ChildId into childParents
                                    from cp in childParents.Where(x => x.IsPrimaryContact).DefaultIfEmpty()
                                    join p in _context.Parents on cp.ParentId equals p.Id into parents
                                    from p in parents.DefaultIfEmpty()
                                    where ep.EventId == eventId
                                    orderby ep.RegisteredAt
                                    select new {
                                        ep.Id,
                                        ep.EventId,
                                        ep.ChildId,
                                        ep.RegisteredAt,
                                        ep.RegisteredBy,
                                        ep.Status,
                                        ep.Notes,
                                        Child = new {
                                            c.Id,
                                            c.FirstName,
                                            c.LastName,
                                            c.ProfilePicture,
                                            Parent = p != null ? new {
                                                p.Id,
                                                p.FirstName,
                                                p.LastName
                                            } : null
                                        }
                                    }).ToListAsync();
            
            return Ok(participants);
        }

        // GET: api/EventParticipants/Child/5
        [HttpGet("Child/{childId}")]
        public async Task<ActionResult<IEnumerable<EventParticipant>>> GetChildParticipations(int childId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userRole == "Parent")
            {
                var parentIdClaim = User.FindFirst("ParentId")?.Value;
                if (int.TryParse(parentIdClaim, out int parentId))
                {
                    var isParent = await _context.ChildParents
                        .AnyAsync(cp => cp.ChildId == childId && cp.ParentId == parentId);
                    if (!isParent)
                        return Forbid();
                }
            }

            return await _context.EventParticipants
                .Where(ep => ep.ChildId == childId)
                .Include(ep => ep.Event)
                .OrderByDescending(ep => ep.RegisteredAt)
                .ToListAsync();
        }

        // POST: api/EventParticipants
        [HttpPost]
        public async Task<ActionResult<EventParticipant>> RegisterParticipant(CreateEventParticipantDto dto)
        {
            Console.WriteLine("*** POST REQUEST RECEIVED - RegisterParticipant called!");
            Console.WriteLine($"*** DTO: EventId={dto.EventId}, ChildId={dto.ChildId}");
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                Console.WriteLine($"=== DEBUG INFO ===");
                Console.WriteLine($"User ID: {userId}");
                Console.WriteLine($"User Role: {userRole}");
                Console.WriteLine($"All Claims:");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"  {claim.Type}: {claim.Value}");
                }

            // Role-based validation
            if (userRole == "Parent")
            {
                var parentIdClaim = User.FindFirst("ParentId")?.Value;
                if (int.TryParse(parentIdClaim, out int parentId))
                {
                    var isParent = await _context.ChildParents
                        .AnyAsync(cp => cp.ChildId == dto.ChildId && cp.ParentId == parentId);
                    if (!isParent)
                        return Forbid("You can only register your own children");
                }
            }

            // Check if already registered
            var existing = await _context.EventParticipants
                .FirstOrDefaultAsync(ep => ep.EventId == dto.EventId && ep.ChildId == dto.ChildId);
            
            if (existing != null)
                return BadRequest("Child is already registered for this event");

            // Check event capacity
            var eventItem = await _context.Events.FindAsync(dto.EventId);
            if (eventItem == null)
                return NotFound("Event not found");

            var currentParticipants = await _context.EventParticipants
                .CountAsync(ep => ep.EventId == dto.EventId && ep.Status == "Registered");

            if (currentParticipants >= eventItem.Capacity)
                return BadRequest("Event is at full capacity");

            // Set status to Registered for all users
            var status = "Registered";
            Console.WriteLine($"User role: {userRole}, Setting status: {status}");
            
            var participant = new EventParticipant
            {
                EventId = dto.EventId,
                ChildId = dto.ChildId,
                Notes = dto.Notes,
                RegisteredBy = userId!,
                RegisteredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Status = status
            };

            _context.EventParticipants.Add(participant);
            await _context.SaveChangesAsync();
            
            Console.WriteLine("*** TEST: PARTICIPANT SUCCESSFULLY CREATED! ***");
            Console.WriteLine($"*** TEST: Participant ID = {participant.Id}, Status = {participant.Status} ***");

            // Create notification for Admin when parent registers child
            if (userRole == "Parent")
            {
                var child = await _context.Children
                    .Include(c => c.Parent)
                    .FirstOrDefaultAsync(c => c.Id == dto.ChildId);
                
                Console.WriteLine($"*** Creating notification for parent registration ***");
                Console.WriteLine($"*** Child: {child?.FirstName} {child?.LastName}, Parent: {child?.Parent?.FirstName} {child?.Parent?.LastName} ***");
                    
                var notification = new Notification
                {
                    Type = "EventRegistration",
                    Title = "New Event Registration",
                    Message = $"{child?.Parent?.FirstName} {child?.Parent?.LastName} registered {child?.FirstName} {child?.LastName} for {eventItem.Name}",
                    RedirectUrl = $"/events/{dto.EventId}/participants",
                    UserId = string.Empty, // Admin notification
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                Console.WriteLine($"*** Notification created for Admin - ID: {notification.Id} ***");
            }

            return CreatedAtAction(nameof(GetEventParticipants), 
                new { eventId = participant.EventId }, participant);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** EXCEPTION: {ex.Message}");
                Console.WriteLine($"*** INNER EXCEPTION: {ex.InnerException?.Message}");
                Console.WriteLine($"*** STACK TRACE: {ex.StackTrace}");
                return BadRequest($"Error registering participant: {ex.Message}. Inner: {ex.InnerException?.Message}");
            }
        }

        // DELETE: api/EventParticipants/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveParticipant(int id)
        {
            var participant = await _context.EventParticipants.FindAsync(id);
            if (participant == null)
                return NotFound();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Role-based validation
            if (userRole == "Parent")
            {
                var parentIdClaim = User.FindFirst("ParentId")?.Value;
                if (int.TryParse(parentIdClaim, out int parentId))
                {
                    var isParent = await _context.ChildParents
                        .AnyAsync(cp => cp.ChildId == participant.ChildId && cp.ParentId == parentId);
                    if (!isParent)
                        return Forbid("You can only remove your own children");
                }
            }

            _context.EventParticipants.Remove(participant);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/EventParticipants/5/approve
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveParticipant(int id)
        {
            var participant = await _context.EventParticipants.FindAsync(id);
            if (participant == null)
                return NotFound();

            participant.Status = "Registered";
            participant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/EventParticipants/5/reject
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectParticipant(int id)
        {
            var participant = await _context.EventParticipants.FindAsync(id);
            if (participant == null)
                return NotFound();

            participant.Status = "Rejected";
            participant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}