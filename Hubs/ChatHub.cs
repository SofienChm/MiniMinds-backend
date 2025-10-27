using DaycareAPI.Data;
using DaycareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DaycareAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ApplicationDbContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendMessage(string recipientId, string content)
        {
            // Resolve sender to a valid AspNetUsers Id
            var senderId = await ResolveCurrentUserIdAsync();
            _logger.LogInformation("[Hub] SendMessage invoked. senderId={SenderId}, recipientId={RecipientId}, contentLength={Length}", senderId, recipientId, content?.Length ?? 0);
            if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(recipientId) || string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("[Hub] Validation failed. senderId or recipientId or content is empty.");
                return;
            }

            // Validate sender and recipient exist in AspNetUsers
            var senderExists = await _context.Users.AnyAsync(u => u.Id == senderId);
            var recipientExists = await _context.Users.AnyAsync(u => u.Id == recipientId);
            _logger.LogInformation("[Hub] User existence check. senderExists={SenderExists}, recipientExists={RecipientExists}", senderExists, recipientExists);
            if (!senderExists || !recipientExists)
            {
                _logger.LogWarning("[Hub] Aborting send. One or both users not found. senderId={SenderId}, recipientId={RecipientId}", senderId, recipientId);
                return;
            }

            var message = new Message
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("[Hub] Message persisted. id={MessageId}, sentAt={SentAt}", message.Id, message.SentAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hub] Message persistence failed. senderId={SenderId}, recipientId={RecipientId}", senderId, recipientId);
                // If DB fails, do NOT broadcast; surface error to caller so client can fallback
                throw new HubException($"Message persistence failed: {ex.Message}", ex);
            }

            var payload = new
            {
                id = message.Id,
                senderId = message.SenderId,
                recipientId = message.RecipientId,
                content = message.Content,
                sentAt = message.SentAt,
                isRead = message.IsRead
            };

            // Send to recipient and echo to sender
            await Clients.User(recipientId).SendAsync("ReceiveMessage", payload);
            await Clients.User(senderId).SendAsync("ReceiveMessage", payload);
        }

        private async Task<string?> ResolveCurrentUserIdAsync()
        {
            var idClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = Context.User?.FindFirst(ClaimTypes.Email)?.Value;

            // If NameIdentifier exists, validate it; if it's an email-like value, convert to Id
            if (!string.IsNullOrEmpty(idClaim))
            {
                // If it matches a real user Id, use it
                if (await _context.Users.AsNoTracking().AnyAsync(u => u.Id == idClaim))
                {
                    return idClaim;
                }

                // If claim looks like an email, attempt lookup
                if (idClaim.Contains('@'))
                {
                    var userFromIdClaim = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == idClaim);
                    _logger.LogInformation("[Hub] NameIdentifier contained email; resolved to Id={ResolvedId}", userFromIdClaim?.Id);
                    if (userFromIdClaim != null) return userFromIdClaim.Id;
                }
            }

            // Fallback to Email claim
            if (!string.IsNullOrEmpty(emailClaim))
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == emailClaim);
                _logger.LogInformation("[Hub] ResolveCurrentUserIdAsync via email. email={Email}, resolvedId={ResolvedId}", emailClaim, user?.Id);
                return user?.Id;
            }

            return null;
        }
    }
}