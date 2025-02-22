using DMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMS.DTOs;
using Microsoft.AspNetCore.Identity;
using DMS.Models;

namespace DMS.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ReportsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var usersList = await _context.Users.ToListAsync();

            var usersDto = new List<UserDTO>();

            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersDto.Add(new UserDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() 
                });
            }

            return Ok(usersDto);
        }
    }

}

