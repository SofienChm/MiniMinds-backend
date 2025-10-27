using System.ComponentModel.DataAnnotations;

namespace DaycareAPI.DTOs
{
    public class UpdateLanguageDto
    {
        [Required]
        [StringLength(10)]
        public string Language { get; set; } = string.Empty;
    }
}
