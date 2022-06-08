using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Domain.Models.AuthenticationModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LoyalWalletv2.Contexts;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        // Database.EnsureDeleted();
        Database.EnsureCreated();
    }

    public DbSet<Customer>? Customers { get; set; }
    public DbSet<Employee>? Employees { get; set; }
    public DbSet<Location>? Locations { get; set; }
    public DbSet<Company>? Companies { get; set; }
    public DbSet<Code>? Codes { get; set; }
    public DbSet<Scan>? Scans { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Customer>()
            .Property(b => b.CountOfStamps)
            .HasField("_countOfStamps");
        
        modelBuilder.Entity<Customer>()
            .Property(b => b.CountOfPurchases)
            .HasField("_countOfPurchases");
        
        modelBuilder.Entity<Customer>()
            .Property(b => b.CountOfGivenPresents)
            .HasField("_countOfGivenPresents");
        
        modelBuilder.Entity<Customer>()
            .Property(b => b.CountOfStoredPresents)
            .HasField("_countOfStoredPresents");
    }
}