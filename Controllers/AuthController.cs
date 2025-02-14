using DMS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private UserManager<AppUser> _userManager;

        public AuthController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost(Name = "register")]
        public async Task<IActionResult> Register()
        {

        }

        //seed roles
    }
}
