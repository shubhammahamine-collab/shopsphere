using Microsoft.EntityFrameworkCore;
using ShopSphere.Domain.Entities;
using System.Collections.Generic;

namespace ShopSphere.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }

    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}