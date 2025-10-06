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
        public DbSet<Folder> Folders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileRecord>()
                .HasIndex(f => f.ObjectKey)
                .IsUnique();

            modelBuilder.Entity<Folder>()
                .HasMany(f => f.SubFolders)
                .WithOne(f => f.Parent)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
