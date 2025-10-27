using DaycareAPI.Data;
using DaycareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Attendance
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetAttendances()
        {
            return await _context.Attendances
                .Include(a => a.Child)
                .ThenInclude(c => c.Parent)
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.CheckInTime)
                .ToListAsync();
        }

        // GET: api/Attendance/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Attendance>> GetAttendance(int id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Child)
                .ThenInclude(c => c.Parent)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
                return NotFound();

            return attendance;
        }

        // GET: api/Attendance/ByChild/5
        [HttpGet("ByChild/{childId}")]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetAttendanceByChild(int childId)
        {
            return await _context.Attendances
                .Where(a => a.ChildId == childId)
                .Include(a => a.Child)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }

        // GET: api/Attendance/ByDate?date=2024-01-01
        [HttpGet("ByDate")]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetAttendanceByDate([FromQuery] DateTime date)
        {
            var targetDate = date.Date;

            return await _context.Attendances
                .Where(a => a.Date.Date == targetDate)
                .Include(a => a.Child)
                .ThenInclude(c => c.Parent)
                .OrderBy(a => a.CheckInTime)
                .ToListAsync();
        }

        // GET: api/Attendance/Today
        [HttpGet("Today")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetTodayAttendance()
        {
            // Return sample attendance data for testing
            var sampleData = new
            {
                totalPresent = 15,
                totalAbsent = 3,
                checkInsToday = 15,
                checkOutsToday = 12
            };
            return Ok(sampleData);
        }

        // POST: api/Attendance/CheckIn
        [HttpPost("CheckIn")]
        public async Task<ActionResult<Attendance>> CheckIn([FromBody] Attendance attendance)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verify child exists
            var childExists = await _context.Children.AnyAsync(c => c.Id == attendance.ChildId);
            if (!childExists)
                return BadRequest(new { message = "Child not found" });

            // Check if already checked in today
            var today = DateTime.UtcNow.Date;
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.ChildId == attendance.ChildId && 
                                         a.Date.Date == today && 
                                         a.CheckOutTime == null);

            if (existingAttendance != null)
                return BadRequest(new { message = "Child is already checked in today" });

            attendance.Date = DateTime.UtcNow.Date;
            attendance.CheckInTime = DateTime.UtcNow;
            attendance.CreatedAt = DateTime.UtcNow;

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
        }

        // POST: api/Attendance/CheckOut/5
        [HttpPost("CheckOut/{id}")]
        public async Task<IActionResult> CheckOut(int id, [FromBody] string? checkOutNotes)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
                return NotFound();

            if (attendance.CheckOutTime != null)
                return BadRequest(new { message = "Child is already checked out" });

            attendance.CheckOutTime = DateTime.UtcNow;
            attendance.CheckOutNotes = checkOutNotes;
            attendance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(attendance);
        }

        // PUT: api/Attendance/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAttendance(int id, Attendance attendance)
        {
            if (id != attendance.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            attendance.UpdatedAt = DateTime.UtcNow;
            _context.Entry(attendance).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttendanceExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Attendance/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
                return NotFound();

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AttendanceExists(int id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }
    }
}