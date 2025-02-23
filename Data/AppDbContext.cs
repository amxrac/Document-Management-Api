using DMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace DMS.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DocumentMetadata> DocumentMetadata { get; set; }
        public DbSet<DocumentContent> DocumentContent { get; set; }
        public DbSet<DocumentTag> DocumentTags { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DocumentMetadata>()
                .HasOne(d => d.DocumentContent)
                .WithOne(c => c.DocumentMetadata)
                .HasForeignKey<DocumentContent>(f => f.DocumentMetadataId);

            modelBuilder.Entity<DocumentMetadata>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId);

            modelBuilder.Entity<DocumentTag>()
                .HasOne(d => d.DocumentMetadata)
                .WithMany(c => c.DocumentTags)
                .HasForeignKey(dt => dt.DocumentMetadataId);

            modelBuilder.Entity<DocumentTag>()
                .HasOne(dt => dt.Tag)
                .WithMany(t => t.DocumentTags)
                .HasForeignKey(dt => dt.TagId);
        }
    }
}
