using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.DTOs
{
    public class CreateEventParticipantDto
    {
        [Required]
        public int EventId { get; set; }
        
        [Required]
        public int ChildId { get; set; }
        
        public string? Notes { get; set; }
    }
}