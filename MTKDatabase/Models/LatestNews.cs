using System.ComponentModel.DataAnnotations;

namespace MTKDatabase.Models
{
    public class LatestNews
    {
        public int Id { get; set; }

        [StringLength(100, MinimumLength = 3)]
        public string? Title { get; set; }
        public string? Image { get; set; }
        public DateOnly NewsTime { get; set; }

        [StringLength(2500, MinimumLength = 3)]
        public string? Description { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        // New property to manage active/inactive state
        public bool IsActive { get; set; } = true;  // Default to active
    }
}
