using DaycareAPI.Data;
using DaycareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEvents()
        {
            var events = await _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new {
                    e.Id,
                    e.Name,
                    e.Type,
                    e.Description,
                    e.Price,
                    e.AgeFrom,
                    e.AgeTo,
                    e.Capacity,
                    e.Time,
                    e.Image,
                    e.CreatedAt,
                    e.UpdatedAt,
                    Participants = (from ep in _context.EventParticipants
                                  join c in _context.Children on ep.ChildId equals c.Id
                                  join p in _context.Parents on c.ParentId equals p.Id
                                  where ep.EventId == e.Id
                                  select new {
                                      ep.Id,
                                      ep.ChildId,
                                      ep.RegisteredAt,
                                      ep.Status,
                                      Child = new {
                                          c.Id,
                                          c.FirstName,
                                          c.LastName,
                                          c.ProfilePicture,
                                          Parent = new {
                                              p.Id,
                                              p.FirstName,
                                              p.LastName
                                          }
                                      }
                                  }).ToList()
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/Events/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem == null)
                return NotFound();

            return eventItem;
        }

        // POST: api/Events
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event eventItem)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            eventItem.CreatedAt = DateTime.UtcNow;
            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            // Create notifications for all parents and teachers
            var parents = await _context.Parents.Where(p => p.IsActive).ToListAsync();
            var teachers = await _context.Teachers.Where(t => t.IsActive).ToListAsync();
            
            var notifications = new List<Notification>();
            
            // Notify parents
            foreach (var parent in parents)
            {
                notifications.Add(new Notification
                {
                    Type = "NewEvent",
                    Title = "New Event Available",
                    Message = $"New event '{eventItem.Name}' has been created. Register your children now!",
                    RedirectUrl = $"/events/{eventItem.Id}",
                    UserId = parent.Id.ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }
            
            // Notify teachers
            foreach (var teacher in teachers)
            {
                notifications.Add(new Notification
                {
                    Type = "NewEvent",
                    Title = "New Event Created",
                    Message = $"New event '{eventItem.Name}' has been created on {eventItem.Time:MMM dd, yyyy}",
                    RedirectUrl = $"/events/{eventItem.Id}",
                    UserId = teacher.Id.ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }
            
            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
                Console.WriteLine($"*** Created {notifications.Count} notifications for new event ***");
                Console.WriteLine($"*** Parent notifications: {parents.Count}, Teacher notifications: {teachers.Count} ***");
                foreach (var n in notifications.Take(3))
                {
                    Console.WriteLine($"*** Sample notification - UserId: {n.UserId}, Type: {n.Type}, Message: {n.Message} ***");
                }
            }
            else
            {
                Console.WriteLine("*** No notifications created - no active parents or teachers found ***");
            }

            return CreatedAtAction(nameof(GetEvent), new { id = eventItem.Id }, eventItem);
        }

        // PUT: api/Events/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, Event eventItem)
        {
            if (id != eventItem.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            eventItem.UpdatedAt = DateTime.UtcNow;
            _context.Entry(eventItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Events/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null)
                return NotFound();

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}