using DMS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DMS.Models;
using DMS.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DMS.Services;

namespace DMS.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly TokenGenerator _tokenGenerator;

        public AuthController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration config, TokenGenerator tokenGenerator)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _tokenGenerator = tokenGenerator;
        }

        [HttpPost("register")]
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

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "Invalid email or password" });
                }
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);


                if (result.Succeeded)
                {
                    var token = await _tokenGenerator.GenerateToken(user);
                    return Ok(new { token = token });
                }

                return BadRequest(new { message = "Invalid email or password" });
            }
            return BadRequest(new
            {
                message = "Requesr failed",
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });

        }

    }
}
