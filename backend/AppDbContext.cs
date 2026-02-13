using backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend;

/* Application database context integrating Identity and custom entities */
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    /* Add DbSet properties for your entities here */
    public DbSet<Destination> Destinations { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /* Configure entity relationships and constraints here */
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Destination>()
            .HasOne(d => d.User)
            .WithMany() // optional: add a collection in ApplicationUser if you want
            .HasForeignKey(d => d.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RefreshToken>()
        .HasIndex(r => r.Token)
        .IsUnique();
    }
}
