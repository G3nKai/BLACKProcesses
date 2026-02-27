using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Data;

public sealed class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<User>();
        users.HasKey(x => x.Id);
        users.HasIndex(x => x.Email).IsUnique();
        users.Property(x => x.Email).HasMaxLength(250).IsRequired();
        users.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
        users.Property(x => x.LastName).HasMaxLength(120).IsRequired();
        users.Property(x => x.Phone).HasMaxLength(25);
    }
}
