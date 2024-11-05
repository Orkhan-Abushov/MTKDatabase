using System.ComponentModel.DataAnnotations;

namespace MTKDatabase.Models
{
    public class ManagementBoardCreateDto
    {

        [Required]
        public int ComplexesId { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }

        [Required]
        [RegularExpression(@"^\+994(50|51|55|99|70|77|60|40|12|88|22|24|36|25|18|23|26)\d{7}$", ErrorMessage = "Phone number must be in the format +994 followed by a valid operator code and 7 digits.")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsMan { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 8)]
        public string Password { get; set; }

    }
}
