using DMS.Models;
using Microsoft.AspNetCore.Identity;

namespace DMS.Data.Seeders
{
    public class AdminSeeder
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;


        public AdminSeeder(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;

        }

        public async Task SeedAdminAsync()
        {
            var adminEmail = "admin@admin.com";
            var adminPassword = "Secure$P@ssw0rd1";

            if (await _userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "System Admin"
                };

                await _userManager.CreateAsync(adminUser, adminPassword);
                if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
