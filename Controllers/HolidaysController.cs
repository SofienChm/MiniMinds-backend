using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DaycareAPI.Data;
using DaycareAPI.Models;
using DaycareAPI.DTOs;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HolidaysController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HolidaysController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Holiday>>> GetHolidays()
        {
            return await _context.Holidays.OrderBy(h => h.Date).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Holiday>> GetHoliday(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();
            return holiday;
        }

        [HttpPost]
        public async Task<ActionResult<Holiday>> CreateHoliday(CreateHolidayDto dto)
        {
            var holiday = new Holiday
            {
                Name = dto.Name,
                Description = dto.Description,
                Date = dto.Date,
                IsRecurring = dto.IsRecurring,
                RecurrenceType = dto.RecurrenceType,
                Color = dto.Color
            };

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetHoliday), new { id = holiday.Id }, holiday);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHoliday(int id, UpdateHolidayDto dto)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            holiday.Name = dto.Name;
            holiday.Description = dto.Description;
            holiday.Date = dto.Date;
            holiday.IsRecurring = dto.IsRecurring;
            holiday.RecurrenceType = dto.RecurrenceType;
            holiday.Color = dto.Color;
            holiday.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHoliday(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}