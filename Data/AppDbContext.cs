using DMS.Models;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Document> Documents { get; set; }
    }
}
