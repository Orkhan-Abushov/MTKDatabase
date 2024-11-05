using System.ComponentModel.DataAnnotations;

namespace MTKDatabase.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [StringLength(100, MinimumLength = 3)]
        public string? Name { get; set; }

        [Required]
        [RegularExpression(@"^\+994(50|51|55|99|70|77|60|40|12|88|22|24|36|25|18|23|26)\d{7}$", ErrorMessage = "Phone number must be in the format +994 followed by a valid operator code and 7 digits.")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email { get; set; }

        [StringLength(2500, MinimumLength = 3)]
        public string? Description { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }

}
