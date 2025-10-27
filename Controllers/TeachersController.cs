using Microsoft.AspNetCore.Mvc;
using DaycareAPI.Models;
using DaycareAPI.DTOs;
using DaycareAPI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeachersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeachersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private static List<object> oldTeachers = new List<object>
        {
            new { 
                id = 1, 
                firstName = "Sarah", 
                lastName = "Johnson", 
                email = "sarah.johnson@daycare.com",
                phone = "555-0101",
                address = "123 Main St",
                dateOfBirth = "1985-03-15",
                hireDate = "2020-08-01",
                specialization = "Early Childhood",
                salary = 45000,
                profilePicture = "https://via.placeholder.com/150",
                isActive = true,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            },
            new { 
                id = 2, 
                firstName = "Michael", 
                lastName = "Davis", 
                email = "michael.davis@daycare.com",
                phone = "555-0102",
                address = "456 Oak Ave",
                dateOfBirth = "1990-07-22",
                hireDate = "2021-01-15",
                specialization = "Special Needs",
                salary = 48000,
                profilePicture = "https://via.placeholder.com/150",
                isActive = true,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            }
        };

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Teacher>>> GetTeachers()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Only Admin can see all teachers
            if (userRole != "Admin")
            {
                return Forbid();
            }
            
            return await _context.Teachers
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Teacher>> GetTeacher(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
                return NotFound();
            return teacher;
        }

        [HttpPost]
        public async Task<ActionResult<Teacher>> CreateTeacher([FromBody] CreateEducatorDto educatorDto)
        {
            Console.WriteLine($"Received CreateTeacher request for: {educatorDto.Email}");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid:");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return BadRequest(ModelState);
            }

            // Check if user with this email already exists
            var existingUser = await _userManager.FindByEmailAsync(educatorDto.Email);
            if (existingUser != null)
            {
                Console.WriteLine($"User with email {educatorDto.Email} already exists");
                return BadRequest(new { message = "A user with this email already exists" });
            }

            // Create user account
            var user = new ApplicationUser
            {
                UserName = educatorDto.Email,
                Email = educatorDto.Email,
                FirstName = educatorDto.FirstName,
                LastName = educatorDto.LastName,
                ProfilePicture = educatorDto.ProfilePicture,
                CreatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"Creating user account for: {user.Email}");
            var userResult = await _userManager.CreateAsync(user, educatorDto.Password);
            if (!userResult.Succeeded)
            {
                Console.WriteLine($"User creation failed: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");
                return BadRequest(userResult.Errors);
            }
            Console.WriteLine($"User created successfully: {user.Id}");

            // Assign Teacher role
            Console.WriteLine($"Assigning Teacher role to user: {user.Id}");
            var roleResult = await _userManager.AddToRoleAsync(user, "Teacher");
            if (!roleResult.Succeeded)
            {
                Console.WriteLine($"Role assignment failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                // If role assignment fails, delete the user
                await _userManager.DeleteAsync(user);
                return BadRequest(roleResult.Errors);
            }
            Console.WriteLine($"Role assigned successfully");

            // Create teacher record
            var teacher = new Teacher
            {
                FirstName = educatorDto.FirstName,
                LastName = educatorDto.LastName,
                Email = educatorDto.Email,
                Phone = educatorDto.Phone,
                Address = educatorDto.Address,
                DateOfBirth = educatorDto.DateOfBirth,
                HireDate = educatorDto.HireDate,
                Specialization = educatorDto.Specialization,
                Salary = educatorDto.Salary,
                ProfilePicture = educatorDto.ProfilePicture,
                CreatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"Creating teacher record for: {teacher.Email}");
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Teacher record created successfully: {teacher.Id}");

            return CreatedAtAction(nameof(GetTeacher), new { id = teacher.Id }, teacher);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeacher(int id, Teacher teacher)
        {
            if (id != teacher.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            teacher.UpdatedAt = DateTime.UtcNow;
            _context.Entry(teacher).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeacherExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
                return NotFound();

            teacher.IsActive = false; // Soft delete
            teacher.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }

        // GET: api/Teachers/5/children
        [HttpGet("{id}/children")]
        public async Task<ActionResult<IEnumerable<Child>>> GetTeacherChildren(int id)
        {
            var teacherExists = await _context.Teachers.AnyAsync(t => t.Id == id);
            if (!teacherExists)
                return NotFound();

            var children = await _context.TeacherChildren
                .Where(tc => tc.TeacherId == id)
                .Include(tc => tc.Child)
                    .ThenInclude(c => c.Parent)
                .Select(tc => tc.Child)
                .ToListAsync();

            return Ok(children);
        }

        // POST: api/Teachers/5/assign-child
        [HttpPost("{id}/assign-child")]
        public async Task<IActionResult> AssignChildToTeacher(int id, [FromBody] AssignChildDto dto)
        {
            var teacherExists = await _context.Teachers.AnyAsync(t => t.Id == id);
            var childExists = await _context.Children.AnyAsync(c => c.Id == dto.ChildId);

            if (!teacherExists || !childExists)
                return BadRequest(new { message = "Teacher or Child not found" });

            var exists = await _context.TeacherChildren
                .AnyAsync(tc => tc.TeacherId == id && tc.ChildId == dto.ChildId);

            if (exists)
                return BadRequest(new { message = "Child already assigned to this teacher" });

            var teacherChild = new TeacherChild
            {
                TeacherId = id,
                ChildId = dto.ChildId,
                AssignedAt = DateTime.UtcNow
            };

            _context.TeacherChildren.Add(teacherChild);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Child assigned successfully" });
        }

        // DELETE: api/Teachers/5/remove-child/3
        [HttpDelete("{id}/remove-child/{childId}")]
        public async Task<IActionResult> RemoveChildFromTeacher(int id, int childId)
        {
            var teacherChild = await _context.TeacherChildren
                .FirstOrDefaultAsync(tc => tc.TeacherId == id && tc.ChildId == childId);

            if (teacherChild == null)
                return NotFound();

            _context.TeacherChildren.Remove(teacherChild);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Child removed successfully" });
        }
    }

    public class AssignChildDto
    {
        public int ChildId { get; set; }
    }
}