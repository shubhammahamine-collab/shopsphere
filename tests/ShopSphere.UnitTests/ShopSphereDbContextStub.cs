
using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions;
using ShopSphere.Domain.Entities;

namespace ShopSphere.UnitTests;

public class ShopSphereDbContextStub : DbContext, IApplicationDbContext
{
    public ShopSphereDbContextStub(
        DbContextOptions<ShopSphereDbContextStub> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<User> Users => Set<User>();

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasQueryFilter(product => product.IsActive);

        base.OnModelCreating(modelBuilder);
    }
}
