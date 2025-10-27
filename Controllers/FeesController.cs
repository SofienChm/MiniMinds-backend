using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DaycareAPI.Data;
using DaycareAPI.Models;
using DaycareAPI.DTOs;

namespace DaycareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/fees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FeeResponseDto>>> GetFees()
        {
            try
            {
                var feesData = await _context.Fees
                    .Include(f => f.Child)
                    .ThenInclude(c => c!.Parent)
                    .Where(f => f.Child != null && f.Child.Parent != null)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                var fees = feesData.Select(f => new FeeResponseDto
                {
                    Id = f.Id,
                    ChildId = f.ChildId,
                    ChildName = f.Child!.FirstName + " " + f.Child.LastName,
                    ParentName = f.Child.Parent!.FirstName + " " + f.Child.Parent.LastName,
                    ParentEmail = f.Child.Parent.Email ?? "",
                    Amount = f.Amount,
                    Description = f.Description,
                    DueDate = f.DueDate,
                    PaidDate = f.PaidDate,
                    Status = f.Status,
                    FeeType = f.FeeType,
                    Notes = f.Notes,
                    DaysOverdue = f.Status == "overdue" ? (int)(DateTime.UtcNow - f.DueDate).TotalDays : 0,
                    CreatedAt = f.CreatedAt
                }).ToList();

                return Ok(fees);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFees: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/fees/child/{childId}
        [HttpGet("child/{childId}")]
        public async Task<ActionResult<IEnumerable<FeeResponseDto>>> GetFeesByChild(int childId)
        {
            var feesData = await _context.Fees
                .Include(f => f.Child)
                .ThenInclude(c => c!.Parent)
                .Where(f => f.ChildId == childId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var fees = feesData.Select(f => new FeeResponseDto
            {
                Id = f.Id,
                ChildId = f.ChildId,
                ChildName = f.Child!.FirstName + " " + f.Child.LastName,
                ParentName = f.Child.Parent!.FirstName + " " + f.Child.Parent.LastName,
                ParentEmail = f.Child.Parent.Email,
                Amount = f.Amount,
                Description = f.Description,
                DueDate = f.DueDate,
                PaidDate = f.PaidDate,
                Status = f.Status,
                FeeType = f.FeeType,
                Notes = f.Notes,
                DaysOverdue = f.Status == "overdue" ? (int)(DateTime.UtcNow - f.DueDate).TotalDays : 0,
                CreatedAt = f.CreatedAt
            }).ToList();

            return Ok(fees);
        }

        // GET: api/fees/parent/{parentId}
        [HttpGet("parent/{parentId}")]
        public async Task<ActionResult<IEnumerable<FeeResponseDto>>> GetFeesByParent(int parentId)
        {
            var feesData = await _context.Fees
                .Include(f => f.Child)
                .ThenInclude(c => c!.Parent)
                .Where(f => f.Child!.ParentId == parentId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var fees = feesData.Select(f => new FeeResponseDto
            {
                Id = f.Id,
                ChildId = f.ChildId,
                ChildName = f.Child!.FirstName + " " + f.Child.LastName,
                ParentName = f.Child.Parent!.FirstName + " " + f.Child.Parent.LastName,
                ParentEmail = f.Child.Parent.Email,
                Amount = f.Amount,
                Description = f.Description,
                DueDate = f.DueDate,
                PaidDate = f.PaidDate,
                Status = f.Status,
                FeeType = f.FeeType,
                Notes = f.Notes,
                DaysOverdue = f.Status == "overdue" ? (int)(DateTime.UtcNow - f.DueDate).TotalDays : 0,
                CreatedAt = f.CreatedAt
            }).ToList();

            return Ok(fees);
        }

        // POST: api/fees
        [HttpPost]
        public async Task<ActionResult<FeeResponseDto>> CreateFee(CreateFeeDto createFeeDto)
        {
            var child = await _context.Children
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == createFeeDto.ChildId);

            if (child == null)
            {
                return NotFound("Child not found");
            }

            var fee = new Fee
            {
                ChildId = createFeeDto.ChildId,
                Amount = createFeeDto.Amount,
                Description = createFeeDto.Description,
                DueDate = createFeeDto.DueDate,
                FeeType = createFeeDto.FeeType,
                Notes = createFeeDto.Notes,
                Status = "pending"
            };

            _context.Fees.Add(fee);
            await _context.SaveChangesAsync();

            // Create notification for parent
            var notification = new Notification
            {
                Type = "NewFee",
                Title = "New Fee Added",
                Message = $"A new fee of ${fee.Amount} for {child.FirstName} {child.LastName} is due on {fee.DueDate:MMM dd, yyyy}. Description: {fee.Description}",
                RedirectUrl = "/fees",
                UserId = child.ParentId.ToString(),
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            Console.WriteLine($"*** Notification created for parent about new fee ***");

            var response = new FeeResponseDto
            {
                Id = fee.Id,
                ChildId = fee.ChildId,
                ChildName = child.FirstName + " " + child.LastName,
                ParentName = child.Parent!.FirstName + " " + child.Parent.LastName,
                ParentEmail = child.Parent.Email,
                Amount = fee.Amount,
                Description = fee.Description,
                DueDate = fee.DueDate,
                PaidDate = fee.PaidDate,
                Status = fee.Status,
                FeeType = fee.FeeType,
                Notes = fee.Notes,
                DaysOverdue = 0,
                CreatedAt = fee.CreatedAt
            };

            return CreatedAtAction(nameof(GetFee), new { id = fee.Id }, response);
        }

        // POST: api/fees/bulk-monthly
        [HttpPost("bulk-monthly")]
        public async Task<ActionResult> CreateMonthlyFeesForAllChildren([FromBody] CreateMonthlyFeesDto dto)
        {
            var children = await _context.Children
                .Include(c => c.Parent)
                .ToListAsync();

            var fees = new List<Fee>();

            foreach (var child in children)
            {
                var fee = new Fee
                {
                    ChildId = child.Id,
                    Amount = dto.Amount,
                    Description = dto.Description,
                    DueDate = dto.DueDate,
                    FeeType = "monthly",
                    Status = "pending"
                };
                fees.Add(fee);
            }

            _context.Fees.AddRange(fees);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Created {fees.Count} monthly fees", count = fees.Count });
        }

        // PUT: api/fees/{id}/pay
        [HttpPut("{id}/pay")]
        public async Task<ActionResult> PayFee(int id, PayFeeDto payFeeDto)
        {
            var fee = await _context.Fees
                .Include(f => f.Child)
                .ThenInclude(c => c!.Parent)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fee == null)
            {
                return NotFound();
            }

            fee.Status = "paid";
            fee.PaidDate = payFeeDto.PaidDate ?? DateTime.UtcNow;
            fee.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(payFeeDto.PaymentNotes))
            {
                fee.Notes = fee.Notes + " | Payment: " + payFeeDto.PaymentNotes;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Fee marked as paid", feeId = id });
        }

        // GET: api/fees/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<FeeResponseDto>> GetFee(int id)
        {
            var fee = await _context.Fees
                .Include(f => f.Child)
                .ThenInclude(c => c!.Parent)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fee == null)
            {
                return NotFound();
            }

            var response = new FeeResponseDto
            {
                Id = fee.Id,
                ChildId = fee.ChildId,
                ChildName = fee.Child!.FirstName + " " + fee.Child.LastName,
                ParentName = fee.Child.Parent!.FirstName + " " + fee.Child.Parent.LastName,
                ParentEmail = fee.Child.Parent.Email,
                Amount = fee.Amount,
                Description = fee.Description,
                DueDate = fee.DueDate,
                PaidDate = fee.PaidDate,
                Status = fee.Status,
                FeeType = fee.FeeType,
                Notes = fee.Notes,
                DaysOverdue = fee.Status == "overdue" ? (int)(DateTime.UtcNow - fee.DueDate).TotalDays : 0,
                CreatedAt = fee.CreatedAt
            };

            return Ok(response);
        }

        // PUT: api/fees/update-overdue
        [HttpPut("update-overdue")]
        public async Task<ActionResult> UpdateOverdueFees()
        {
            var overdueFees = await _context.Fees
                .Where(f => f.Status == "pending" && f.DueDate < DateTime.UtcNow)
                .ToListAsync();

            foreach (var fee in overdueFees)
            {
                fee.Status = "overdue";
                fee.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Updated {overdueFees.Count} fees to overdue status", count = overdueFees.Count });
        }

        // GET: api/fees/summary
        [HttpGet("summary")]
        public async Task<ActionResult> GetFeesSummary()
        {
            var totalFees = await _context.Fees.CountAsync();
            var paidFees = await _context.Fees.CountAsync(f => f.Status == "paid");
            var pendingFees = await _context.Fees.CountAsync(f => f.Status == "pending");
            var overdueFees = await _context.Fees.CountAsync(f => f.Status == "overdue");
            
            var totalAmount = await _context.Fees.SumAsync(f => f.Amount);
            var paidAmount = await _context.Fees.Where(f => f.Status == "paid").SumAsync(f => f.Amount);
            var pendingAmount = await _context.Fees.Where(f => f.Status == "pending").SumAsync(f => f.Amount);
            var overdueAmount = await _context.Fees.Where(f => f.Status == "overdue").SumAsync(f => f.Amount);

            return Ok(new
            {
                totalFees,
                paidFees,
                pendingFees,
                overdueFees,
                totalAmount,
                paidAmount,
                pendingAmount,
                overdueAmount
            });
        }

        // DELETE: api/fees/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteFee(int id)
        {
            var fee = await _context.Fees.FindAsync(id);
            if (fee == null)
            {
                return NotFound();
            }

            _context.Fees.Remove(fee);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Fee deleted successfully" });
        }
    }

    public class CreateMonthlyFeesDto
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
    }
}