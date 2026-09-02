using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions;
using ShopSphere.Application.Features.Products.DTOs;
using ShopSphere.Application.Features.Products.Requests;
using ShopSphere.Domain.Entities;

namespace ShopSphere.Application.Features.Products.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;

    public ProductService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            SKU = request.SKU,
            Price = request.Price,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Price = product.Price
        };
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Select(product => new ProductDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                SKU = product.SKU,
                Price = product.Price
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                SKU = product.SKU,
                Price = product.Price
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDto?> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.SKU = request.SKU;
        product.Price = request.Price;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Price = product.Price
        };
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return false;
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ProductDto?> RestoreAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        if (product.IsActive)
        {
            return new ProductDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                SKU = product.SKU,
                Price = product.Price
            };
        }

        product.IsActive = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Price = product.Price
        };
    }
}