using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DaycareAPI.Data;
using DaycareAPI.Models;
using DaycareAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace DaycareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(ApplicationDbContext context, ILogger<MessagesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("conversations")]
        public async Task<ActionResult> GetConversations()
        {
            try
            {
                // Resolve current user Id strictly to a valid AspNetUsers Id
                var currentUserId = await ResolveCurrentUserIdAsync();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _logger.LogWarning("[API] GetConversations unauthorized: missing user id claim.");
                    return Unauthorized();
                }

                _logger.LogInformation("[API] GetConversations for user {UserId}", currentUserId);

                // EF-friendly projection to avoid grouping with navigation properties
                var baseQuery = _context.Messages
                    .Where(m => m.SenderId == currentUserId || m.RecipientId == currentUserId)
                    .Select(m => new
                    {
                        OtherUserId = m.SenderId == currentUserId ? m.RecipientId : m.SenderId,
                        m.Id,
                        m.Content,
                        m.SentAt,
                        m.IsRead,
                        m.RecipientId
                    });

                var grouped = await baseQuery
                    .GroupBy(x => x.OtherUserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        LastSentAt = g.Max(x => x.SentAt),
                        LastMessageId = g.OrderByDescending(x => x.SentAt).Select(x => x.Id).FirstOrDefault(),
                        UnreadCount = g.Count(x => x.RecipientId == currentUserId && !x.IsRead)
                    })
                    .OrderByDescending(c => c.LastSentAt)
                    .ToListAsync();

                var lastIds = grouped.Select(g => g.LastMessageId).Where(id => id != 0).ToList();
                var lastDetails = await _context.Messages
                    .Where(m => lastIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.Content, m.SentAt })
                    .ToListAsync();

                var userIds = grouped.Select(g => g.UserId).ToList();
                var users = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                    .ToListAsync();

                var conversations = grouped.Select(g => new
                {
                    userId = g.UserId,
                    user = users.FirstOrDefault(u => u.Id == g.UserId),
                    lastMessage = lastDetails.FirstOrDefault(l => l.Id == g.LastMessageId),
                    unreadCount = g.UnreadCount
                }).ToList();

                return Ok(conversations);
            }
            catch (Exception ex)
            {
                // Return empty conversations if database not ready
                _logger.LogError(ex, "[API] GetConversations failed; returning empty list.");
                return Ok(new object[0]);
            }
        }

        [HttpGet("conversation/{userId}")]
        public async Task<ActionResult> GetConversation(string userId)
        {
            try
            {
                var currentUserId = await ResolveCurrentUserIdAsync();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _logger.LogWarning("[API] GetConversation unauthorized: missing user id.");
                    return Unauthorized();
                }

                _logger.LogInformation("[API] GetConversation between {CurrentUserId} and {UserId}", currentUserId, userId);
                var messages = await _context.Messages
                    .Where(m => (m.SenderId == currentUserId && m.RecipientId == userId) ||
                               (m.SenderId == userId && m.RecipientId == currentUserId))
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception)
            {
                // Return empty messages if database not ready
                _logger.LogError("[API] GetConversation failed; returning empty list.");
                return Ok(new object[0]);
            }
        }

        [HttpPost]
        public async Task<ActionResult> SendMessage([FromBody] SendMessageDto messageDto)
        {
            if (string.IsNullOrEmpty(messageDto.Content) || string.IsNullOrEmpty(messageDto.RecipientId))
            {
                _logger.LogWarning("[API] SendMessage bad request. content or recipientId missing.");
                return BadRequest(new { error = "Content and RecipientId are required" });
            }

            try
            {
                // Resolve current user Id to a valid AspNetUsers Id
                var currentUserId = await ResolveCurrentUserIdAsync();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _logger.LogWarning("[API] SendMessage unauthorized: missing user id.");
                    return Unauthorized();
                }

                _logger.LogInformation("[API] SendMessage attempt by {SenderId} to {RecipientId}. contentLength={Length}", currentUserId, messageDto.RecipientId, messageDto.Content.Length);
                // Validate sender exists
                var senderExists = await _context.Users.AnyAsync(u => u.Id == currentUserId);
                if (!senderExists)
                {
                    _logger.LogWarning("[API] SendMessage sender not found: {SenderId}", currentUserId);
                    return BadRequest(new { error = "Sender not found" });
                }

                // Validate recipient exists
                var recipientExists = await _context.Users.AnyAsync(u => u.Id == messageDto.RecipientId);
                if (!recipientExists)
                {
                    _logger.LogWarning("[API] SendMessage recipient not found: {RecipientId}", messageDto.RecipientId);
                    return BadRequest(new { error = "Recipient not found" });
                }

                var message = new Message
                {
                    SenderId = currentUserId,
                    RecipientId = messageDto.RecipientId,
                    Content = messageDto.Content,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[API] SendMessage persisted. id={MessageId}, sentAt={SentAt}", message.Id, message.SentAt);

                return Ok(new { success = true, messageId = message.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] SendMessage failed.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        private async Task<string?> ResolveCurrentUserIdAsync()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var emailClaim = User.FindFirstValue(ClaimTypes.Email);

            // If NameIdentifier exists, validate it; if it's email-like, convert to Id
            if (!string.IsNullOrEmpty(idClaim))
            {
                if (await _context.Users.AsNoTracking().AnyAsync(u => u.Id == idClaim))
                {
                    return idClaim;
                }

                if (idClaim.Contains('@'))
                {
                    var userFromIdClaim = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == idClaim);
                    _logger.LogInformation("[API] NameIdentifier contained email; resolved to Id={ResolvedId}", userFromIdClaim?.Id);
                    if (userFromIdClaim != null) return userFromIdClaim.Id;
                }
            }

            if (!string.IsNullOrEmpty(emailClaim))
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == emailClaim);
                _logger.LogInformation("[API] ResolveCurrentUserIdAsync via email. email={Email}, resolvedId={ResolvedId}", emailClaim, user?.Id);
                return user?.Id;
            }

            return null;
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFound();

            message.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("users")]
        public async Task<ActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    Email = u.Email,
                    Role = "Admin" // Simplified for now
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}