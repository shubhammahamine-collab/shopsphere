using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopSphere.Application.Abstractions;
using ShopSphere.Application.Features.Products.DTOs;
using ShopSphere.Application.Features.Products.Requests;
using ShopSphere.Domain.Entities;

namespace ShopSphere.Application.Features.Products.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IApplicationDbContext context,
        ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating product {ProductName} with SKU {SKU}.",
            request.Name,
            request.SKU);

        if (request.StockQuantity < 0)
        {
            _logger.LogWarning(
                "Invalid stock quantity {StockQuantity} while creating product with SKU {SKU}.",
                request.StockQuantity,
                request.SKU);

            throw new ArgumentException(
                "Stock quantity cannot be negative.");
        }

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            SKU = request.SKU,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product {ProductId} created successfully with SKU {SKU}.",
            product.Id,
            product.SKU);

        return MapToDto(product);
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
                Price = product.Price,
                StockQuantity = product.StockQuantity
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
                Price = product.Price,
                StockQuantity = product.StockQuantity
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
            _logger.LogWarning(
                "Product {ProductId} was not found for update.",
                id);

            return null;
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.SKU = request.SKU;
        product.Price = request.Price;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product {ProductId} updated successfully.",
            product.Id);

        return MapToDto(product);
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
            _logger.LogWarning(
                "Product {ProductId} was not found for deletion.",
                id);

            return false;
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product {ProductId} soft deleted successfully.",
            product.Id);

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
            _logger.LogWarning(
                "Product {ProductId} was not found for restore.",
                id);

            return null;
        }

        if (product.IsActive)
        {
            return MapToDto(product);
        }

        product.IsActive = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product {ProductId} restored successfully.",
            product.Id);

        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateStockAsync(
        int id,
        UpdateStockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity < 0)
        {
            _logger.LogWarning(
                "Invalid stock quantity {StockQuantity} for product {ProductId}.",
                request.Quantity,
                id);

            throw new ArgumentException(
                "Stock quantity cannot be negative.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (product == null)
        {
            _logger.LogWarning(
                "Product {ProductId} was not found for stock update.",
                id);

            throw new KeyNotFoundException(
                $"Product with ID {id} not found.");
        }

        var oldStockQuantity = product.StockQuantity;

        product.StockQuantity = request.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product {ProductId} stock updated from {OldStockQuantity} to {NewStockQuantity}.",
            product.Id,
            oldStockQuantity,
            product.StockQuantity);

        return MapToDto(product);
    }

    private ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        };
    }
}