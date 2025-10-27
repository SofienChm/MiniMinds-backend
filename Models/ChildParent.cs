using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaycareAPI.Models
{
    public class ChildParent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChildId { get; set; }

        [Required]
        public int ParentId { get; set; }

        [StringLength(50)]
        public string? RelationshipType { get; set; } // e.g., "Mother", "Father", "Guardian"

        public bool IsPrimaryContact { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ChildId")]
        public Child? Child { get; set; }

        [ForeignKey("ParentId")]
        public Parent? Parent { get; set; }
    }
}
