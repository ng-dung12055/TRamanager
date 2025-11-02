using Microsoft.EntityFrameworkCore;
using TRManager.Api.Data.Entities;

namespace TRManager.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(u => u.Id);
                e.Property(u => u.Email).IsRequired().HasMaxLength(256);
                e.Property(u => u.UserName).IsRequired().HasMaxLength(100);
                e.HasIndex(u => u.Email).IsUnique();

                e.HasMany(u => u.Roles)
                 .WithMany(r => r.Users)
                 .UsingEntity(j => j.ToTable("UserRoles"));
            });

            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("Roles");
                e.HasKey(r => r.Id);
                e.Property(r => r.Name).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.ToTable("RefreshTokens");
                e.HasKey(t => t.Id);
                e.HasOne(t => t.User)
                 .WithMany(u => u.RefreshTokens)
                 .HasForeignKey(t => t.UserId);
            });
        }
    }
}
