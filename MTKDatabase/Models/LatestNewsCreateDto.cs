using System.ComponentModel.DataAnnotations;

namespace MTKDatabase.Models
{
    public class LatestNewsCreateDto
    {
        [StringLength(100, MinimumLength = 3)]
        public string? Title { get; set; }
        public string? Image { get; set; }

        [Required]
        public DateOnly NewsTime { get; set; }

        [StringLength(2500, MinimumLength = 3)]
        public string? Description { get; set; }

    }
}
