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
    public class ProgramsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProgramsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DaycareProgram>>> GetPrograms()
        {
            return await _context.DaycarePrograms
                .Include(p => p.Enrollments)
                    .ThenInclude(e => e.Child)
                .OrderBy(p => p.Date)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DaycareProgram>> GetProgram(int id)
        {
            var program = await _context.DaycarePrograms
                .Include(p => p.Enrollments)
                    .ThenInclude(e => e.Child)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (program == null)
                return NotFound();

            return program;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<DaycareProgram>> CreateProgram(ProgramDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var program = new DaycareProgram
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Capacity = dto.Capacity,
                    MinAge = dto.MinAge,
                    MaxAge = dto.MaxAge,
                    Date = DateTime.Parse(dto.Date),
                    StartTime = TimeSpan.Parse(dto.StartTime + ":00"),
                    EndTime = TimeSpan.Parse(dto.EndTime + ":00"),
                    CreatedAt = DateTime.UtcNow
                };

                _context.DaycarePrograms.Add(program);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> UpdateProgram(int id, DaycareProgram program)
        {
            if (id != program.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            program.UpdatedAt = DateTime.UtcNow;
            _context.Entry(program).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProgramExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProgram(int id)
        {
            var program = await _context.DaycarePrograms.FindAsync(id);
            if (program == null)
                return NotFound();

            _context.DaycarePrograms.Remove(program);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/enroll/{childId}")]
        public async Task<IActionResult> EnrollChild(int id, int childId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Parents can only enroll their own children
            if (userRole == "Parent")
            {
                var parentIdClaim = User.FindFirst("ParentId")?.Value;
                if (int.TryParse(parentIdClaim, out int parentId))
                {
                    var child = await _context.Children.FirstOrDefaultAsync(c => c.Id == childId && c.ParentId == parentId);
                    if (child == null)
                        return Forbid("You can only enroll your own children");
                }
                else
                {
                    return Forbid();
                }
            }
            
            var program = await _context.DaycarePrograms
                .Include(p => p.Enrollments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (program == null)
                return NotFound("Program not found");

            var childToEnroll = await _context.Children.FindAsync(childId);
            if (childToEnroll == null)
                return NotFound("Child not found");

            // Check if already enrolled
            if (program.Enrollments.Any(e => e.ChildId == childId))
                return BadRequest("Child is already enrolled in this program");

            // Check capacity
            if (program.Enrollments.Count >= program.Capacity)
                return BadRequest("Program is at full capacity");

            // Check age eligibility
            var age = DateTime.Now.Year - childToEnroll.DateOfBirth.Year;
            if (age < program.MinAge || age > program.MaxAge)
                return BadRequest($"Child age ({age}) is not within the program age range ({program.MinAge}-{program.MaxAge})");

            var enrollment = new ProgramEnrollment
            {
                ProgramId = id,
                ChildId = childId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.ProgramEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id}/unenroll/{childId}")]
        public async Task<IActionResult> UnenrollChild(int id, int childId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Parents can only unenroll their own children
            if (userRole == "Parent")
            {
                var parentIdClaim = User.FindFirst("ParentId")?.Value;
                if (int.TryParse(parentIdClaim, out int parentId))
                {
                    var child = await _context.Children.FirstOrDefaultAsync(c => c.Id == childId && c.ParentId == parentId);
                    if (child == null)
                        return Forbid("You can only unenroll your own children");
                }
                else
                {
                    return Forbid();
                }
            }
            
            var enrollment = await _context.ProgramEnrollments
                .FirstOrDefaultAsync(e => e.ProgramId == id && e.ChildId == childId);

            if (enrollment == null)
                return NotFound("Enrollment not found");

            _context.ProgramEnrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool ProgramExists(int id)
        {
            return _context.DaycarePrograms.Any(e => e.Id == id);
        }
    }
}