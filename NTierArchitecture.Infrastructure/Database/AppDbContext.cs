using Microsoft.EntityFrameworkCore;
using NTierArchitecture.Domain.Entities;
using NTierArchitecture.Domain.Enums;

namespace NTierArchitecture.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> User { get; set; }
        public DbSet<Role> Role { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(role => role.Id);
                entity.Property(role => role.RoleName)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.HasIndex(role => role.RoleName)
                    .IsUnique();

                entity.HasData(
                    new Role { Id = 1, RoleName = "Admin" },
                    new Role { Id = 2, RoleName = "User" });
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(user => user.Id);
                entity.Property(user => user.UserName)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(user => user.Email)
                    .IsRequired()
                    .HasMaxLength(512);
                entity.Property(user => user.Password)
                    .IsRequired();
                entity.HasIndex(user => user.UserName)
                    .IsUnique();
                entity.HasIndex(user => user.Email)
                    .IsUnique();
                entity.HasOne(user => user.Role)
                    .WithMany(role => role.Users)
                    .HasForeignKey(user => user.RoleId);

                entity.HasData(
                    new User
                    {
                        // admin@example.com
                        // @Admin123
                        Id = new Guid("9a6c1f69-6df7-4dc3-8f34-d6495a2cb001"),
                        RoleId = 1,
                        UserName = "admin",
                        Email = "v1:/H+QNjNCknHMBv9D:/zFq3eWAgM4h3lBivDyfJg==:96kjhKcbGze/LcVY1Byt8rU=",
                        Password = "$2a$12$fB5O5M3pc63XgOnDXvnYQejqtHN7urLS/b/qEJfOqKv92eZUIwU2W",
                        Status = UserStatus.Active,
                        CreationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        IsDeleted = false
                    },
                    new User
                    {
                        // user@example.com
                        // @User123
                        Id = new Guid("f604b6a6-5096-4738-8cda-eab8c0b17002"),
                        RoleId = 2,
                        UserName = "user",
                        Email = "v1:pLqf8kWzNoMXWD58:hwxwrvGxFOhpnsXk2eZnFQ==:3WPTV0KaxiMV6T3lilknzw==",
                        Password = "$2a$12$ZfUFz2IGpfedCLScjZoUR.EncprsmG3HW.zXFVVLdFhTyybfLjtCe",
                        Status = UserStatus.Active,
                        CreationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        IsDeleted = false
                    });
            });
        }
    }
}
