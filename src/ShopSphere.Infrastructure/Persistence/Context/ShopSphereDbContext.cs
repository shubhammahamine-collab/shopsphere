using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions;
using ShopSphere.Domain.Entities;

namespace ShopSphere.Infrastructure.Persistence.Context;

public class ShopSphereDbContext : DbContext, IApplicationDbContext
{
    public ShopSphereDbContext(
        DbContextOptions<ShopSphereDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ShopSphereDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}