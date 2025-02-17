using System.ComponentModel.DataAnnotations;

namespace DMS.ViewModels
{
    public class LoginVM
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
