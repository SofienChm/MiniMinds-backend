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
    public class DailyActivitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DailyActivitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DailyActivity>>> GetActivities([FromQuery] string? date)
        {
            var query = _context.DailyActivities.Include(a => a.Child).AsQueryable();

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime filterDate))
            {
                query = query.Where(a => a.ActivityTime.Date == filterDate.Date);
            }

            return await query.OrderByDescending(a => a.ActivityTime).ToListAsync();
        }

        [HttpGet("child/{childId}")]
        public async Task<ActionResult<IEnumerable<DailyActivity>>> GetActivitiesByChild(int childId, [FromQuery] string? date)
        {
            var query = _context.DailyActivities
                .Where(a => a.ChildId == childId)
                .Include(a => a.Child)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime filterDate))
            {
                query = query.Where(a => a.ActivityTime.Date == filterDate.Date);
            }

            return await query.OrderByDescending(a => a.ActivityTime).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DailyActivity>> GetActivity(int id)
        {
            var activity = await _context.DailyActivities
                .Include(a => a.Child)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
                return NotFound();

            return activity;
        }

        [HttpPost]
        public async Task<ActionResult<DailyActivity>> CreateActivity(DailyActivity activity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            activity.CreatedAt = DateTime.UtcNow;
            _context.DailyActivities.Add(activity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetActivity), new { id = activity.Id }, activity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateActivity(int id, DailyActivity activity)
        {
            if (id != activity.Id)
                return BadRequest();

            _context.Entry(activity).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActivityExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActivity(int id)
        {
            var activity = await _context.DailyActivities.FindAsync(id);
            if (activity == null)
                return NotFound();

            _context.DailyActivities.Remove(activity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ActivityExists(int id)
        {
            return _context.DailyActivities.Any(e => e.Id == id);
        }
    }
}
