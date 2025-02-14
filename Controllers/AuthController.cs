using DMS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DMS.Models;
using DMS.ViewModels;

namespace DMS.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AuthController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost(Name = "register")]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = new()
                {
                    Name = model.Name,
                    Email = model.Email,
                    UserName = model.Email
                };
                var result = await _userManager.CreateAsync(user, model.Password!);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Editor");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return Ok(new
                    {
                        message = "User registered successfully",
                        user = new
                        {
                            email = user.Email,
                            role = "Editor"
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        message = "User registration failed",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }                               
            }
            return BadRequest(new
            {
                message = "Validation failed",
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

    }
}
