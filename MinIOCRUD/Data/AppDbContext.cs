using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Models;

namespace MinIOCRUD.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
           
        }

        public DbSet<FileRecord> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileRecord>()
                .HasIndex(f => f.ObjectKey)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
