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
    public class LeavesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeavesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentTeacherId()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Teacher")
            {
                var teacherIdStr = User.FindFirst("TeacherId")?.Value;
                if (int.TryParse(teacherIdStr, out var teacherId))
                    return teacherId;
            }
            return null;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public class CreateLeaveRequestDto
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string? Reason { get; set; }
        }

        public class AdminCreateLeaveRequestDto
        {
            public int TeacherId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string? Reason { get; set; }
            public bool Approve { get; set; } = true;
        }

        [HttpPost("request")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> RequestLeave([FromBody] CreateLeaveRequestDto dto)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == null)
                return Forbid();

            if (dto.StartDate.Date > dto.EndDate.Date)
                return BadRequest(new { message = "End date must be on or after start date." });

            var days = (int)(dto.EndDate.Date - dto.StartDate.Date).TotalDays + 1;
            if (days <= 0)
                return BadRequest(new { message = "Invalid leave duration." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId.Value);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found." });

            var request = new LeaveRequest
            {
                TeacherId = teacherId.Value,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                Days = days,
                Reason = dto.Reason?.Trim(),
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMyRequests), new { id = request.Id }, request);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetMyRequests()
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == null)
                return Forbid();

            var requests = await _context.LeaveRequests
                .Where(r => r.TeacherId == teacherId.Value)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var query = _context.LeaveRequests
                .Include(r => r.Teacher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateForTeacher([FromBody] AdminCreateLeaveRequestDto dto)
        {
            if (dto.StartDate.Date > dto.EndDate.Date)
                return BadRequest(new { message = "End date must be on or after start date." });

            var days = (int)(dto.EndDate.Date - dto.StartDate.Date).TotalDays + 1;
            if (days <= 0)
                return BadRequest(new { message = "Invalid leave duration." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == dto.TeacherId);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found." });

            var request = new LeaveRequest
            {
                TeacherId = teacher.Id,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                Days = days,
                Reason = dto.Reason?.Trim(),
                Status = dto.Approve ? "Approved" : "Pending",
                RequestedAt = DateTime.UtcNow,
                ApprovedAt = null,
                ApprovedByUserId = null
            };

            if (dto.Approve)
            {
                var year = request.StartDate.Year;
                var usedDays = await _context.LeaveRequests
                    .Where(r => r.TeacherId == teacher.Id && r.Status == "Approved" && r.StartDate.Year == year)
                    .SumAsync(r => (int?)r.Days) ?? 0;

                if (usedDays + request.Days > teacher.AnnualLeaveDays)
                {
                    return BadRequest(new { message = "Insufficient leave balance.", remaining = teacher.AnnualLeaveDays - usedDays });
                }

                request.ApprovedAt = DateTime.UtcNow;
                request.ApprovedByUserId = GetCurrentUserId();
            }

            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = request.Id }, request);
        }

        [HttpPut("{id:int}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound(new { message = "Leave request not found." });
            if (request.Status != "Pending") return BadRequest(new { message = "Only pending requests can be approved." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == request.TeacherId);
            if (teacher == null) return NotFound(new { message = "Teacher not found." });

            var year = request.StartDate.Year;
            var usedDays = await _context.LeaveRequests
                .Where(r => r.TeacherId == teacher.Id && r.Status == "Approved" && r.StartDate.Year == year)
                .SumAsync(r => (int?)r.Days) ?? 0;

            if (usedDays + request.Days > teacher.AnnualLeaveDays)
            {
                return BadRequest(new { message = "Insufficient leave balance.", remaining = teacher.AnnualLeaveDays - usedDays });
            }

            request.Status = "Approved";
            request.ApprovedAt = DateTime.UtcNow;
            request.ApprovedByUserId = GetCurrentUserId();

            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpPut("{id:int}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound(new { message = "Leave request not found." });
            if (request.Status != "Pending") return BadRequest(new { message = "Only pending requests can be rejected." });

            request.Status = "Rejected";
            request.ApprovedAt = DateTime.UtcNow;
            request.ApprovedByUserId = GetCurrentUserId();

            await _context.SaveChangesAsync();
            return Ok(request);
        }

        public class LeaveBalanceDto
        {
            public int AnnualAllocation { get; set; }
            public int UsedDays { get; set; }
            public int RemainingDays { get; set; }
        }

        [HttpGet("balance")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetMyBalance()
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == null) return Forbid();

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId.Value);
            if (teacher == null) return NotFound(new { message = "Teacher not found." });

            var year = DateTime.UtcNow.Year;
            var usedDays = await _context.LeaveRequests
                .Where(r => r.TeacherId == teacher.Id && r.Status == "Approved" && r.StartDate.Year == year)
                .SumAsync(r => (int?)r.Days) ?? 0;

            var dto = new LeaveBalanceDto
            {
                AnnualAllocation = teacher.AnnualLeaveDays,
                UsedDays = usedDays,
                RemainingDays = Math.Max(teacher.AnnualLeaveDays - usedDays, 0)
            };

            return Ok(dto);
        }
    }
}