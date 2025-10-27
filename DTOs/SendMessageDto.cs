namespace DaycareAPI.DTOs
{
    public class SendMessageDto
    {
        public string RecipientId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}