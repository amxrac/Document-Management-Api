using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DMS.Models
{
    public class AppUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
