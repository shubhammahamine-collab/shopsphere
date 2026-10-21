using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShopSphere.Application.Features.Products.Requests;
using ShopSphere.Application.Features.Products.Services;
using ShopSphere.Domain.Entities;

namespace ShopSphere.UnitTests.Products;

public class ProductServiceTests
{
    private static (ShopSphereDbContextStub Context, ProductService Service)
        CreateService()
    {
        var options = new DbContextOptionsBuilder<ShopSphereDbContextStub>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ShopSphereDbContextStub(options);
        var logger = Mock.Of<ILogger<ProductService>>();

        var service = new ProductService(context, logger);

        return (context, service);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesProduct()
    {
        // Arrange
        var (context, service) = CreateService();

        var request = new CreateProductRequest
        {
            CategoryId = 2,
            Name = "Test Mouse",
            Description = "Unit test product",
            SKU = "TEST-MOUSE-001",
            Price = 799m,
            StockQuantity = 10
        };

        // Act
        var result = await service.CreateAsync(
            request, CancellationToken.None);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("Test Mouse", result.Name);
        Assert.Equal("TEST-MOUSE-001", result.SKU);
        Assert.Equal(799m, result.Price);
        Assert.Equal(10, result.StockQuantity);

        var savedProduct = await context.Products
            .FirstOrDefaultAsync(p => p.SKU == "TEST-MOUSE-001");

        Assert.NotNull(savedProduct);
        Assert.True(savedProduct.IsActive);
    }

    [Fact]
    public async Task CreateAsync_NegativeStock_ThrowsArgumentException()
    {
        // Arrange
        var (_, service) = CreateService();

        var request = new CreateProductRequest
        {
            CategoryId = 2,
            Name = "Invalid Product",
            Description = "Invalid stock test",
            SKU = "INVALID-STOCK-001",
            Price = 100m,
            StockQuantity = -1
        };

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request, CancellationToken.None));

        // Assert
        Assert.Equal("Stock quantity cannot be negative.", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsProduct()
    {
        // Arrange
        var (context, service) = CreateService();

        context.Products.Add(new Product
        {
            CategoryId = 2,
            Name = "Test Keyboard",
            SKU = "TEST-KEYBOARD-001",
            Price = 1499m,
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var product = await context.Products
            .FirstAsync(p => p.SKU == "TEST-KEYBOARD-001");

        // Act
        var result = await service.GetByIdAsync(
            product.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Keyboard", result.Name);
        Assert.Equal("TEST-KEYBOARD-001", result.SKU);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingProduct_ReturnsNull()
    {
        // Arrange
        var (_, service) = CreateService();

        // Act
        var result = await service.GetByIdAsync(
            9999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }


    [Fact]
    public async Task UpdateAsync_ExistingProduct_UpdatesProduct()
    {
        // Arrange
        var (context, service) = CreateService();

        var product = new Product
        {
            CategoryId = 2,
            Name = "Old Mouse",
            SKU = "MOUSE-OLD",
            Price = 500m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var request = new UpdateProductRequest
        {
            CategoryId = 2,
            Name = "Updated Mouse",
            Description = "Updated description",
            SKU = "MOUSE-NEW",
            Price = 750m
        };

        // Act
        var result = await service.UpdateAsync(
            product.Id, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Mouse", result.Name);
        Assert.Equal("MOUSE-NEW", result.SKU);
        Assert.Equal(750m, result.Price);
    }

    [Fact]
    public async Task UpdateAsync_MissingProduct_ReturnsNull()
    {
        // Arrange
        var (_, service) = CreateService();

        var request = new UpdateProductRequest
        {
            CategoryId = 2,
            Name = "Updated Mouse",
            Description = "Updated description",
            SKU = "MOUSE-NEW",
            Price = 750m
        };

        // Act
        var result = await service.UpdateAsync(
            9999, request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingProduct_SoftDeletesProduct()
    {
        // Arrange
        var (context, service) = CreateService();

        var product = new Product
        {
            CategoryId = 2,
            Name = "Delete Test Mouse",
            SKU = "DELETE-MOUSE",
            Price = 500m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(
            product.Id, CancellationToken.None);

        // Assert
        Assert.True(result);

        var savedProduct = await context.Products
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == product.Id);

        Assert.False(savedProduct.IsActive);
    }

    [Fact]
    public async Task UpdateStockAsync_NegativeQuantity_ThrowsArgumentException()
    {
        // Arrange
        var (_, service) = CreateService();

        var request = new UpdateStockRequest
        {
            Quantity = -5
        };

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateStockAsync(
                1, request, CancellationToken.None));

        // Assert
        Assert.Equal("Stock quantity cannot be negative.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingProduct_ReturnsFalse()
    {
        var (_, service) = CreateService();

        var result = await service.DeleteAsync(
            9999,
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_DeletedProduct_RestoresProduct()
    {
        var (context, service) = CreateService();

        var product = new Product
        {
            CategoryId = 2,
            Name = "Deleted Mouse",
            SKU = "DELETED-MOUSE-001",
            Price = 599m,
            StockQuantity = 5,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var result = await service.RestoreAsync(
            product.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Deleted Mouse", result.Name);

        var restoredProduct = await context.Products
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == product.Id);

        Assert.True(restoredProduct.IsActive);
    }

    [Fact]
    public async Task UpdateStockAsync_ValidQuantity_UpdatesStock()
    {
        var (context, service) = CreateService();

        var product = new Product
        {
            CategoryId = 2,
            Name = "Stock Test Mouse",
            SKU = "STOCK-MOUSE-001",
            Price = 799m,
            StockQuantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var result = await service.UpdateStockAsync(
            product.Id,
            new UpdateStockRequest { Quantity = 20 },
            CancellationToken.None);

        Assert.Equal(20, result.StockQuantity);

        var updatedProduct = await context.Products
            .FirstAsync(p => p.Id == product.Id);

        Assert.Equal(20, updatedProduct.StockQuantity);
    }

    [Fact]
    public async Task UpdateStockAsync_MissingProduct_ThrowsKeyNotFoundException()
    {
        var (_, service) = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateStockAsync(
                9999,
                new UpdateStockRequest { Quantity = 10 },
                CancellationToken.None));
    }

    [Fact]
    public async Task GetAllAsync_ExistingProducts_ReturnsProducts()
    {
        var (context, service) = CreateService();

        context.Products.AddRange(
            new Product
            {
                CategoryId = 2,
                Name = "Test Mouse",
                SKU = "LIST-MOUSE-001",
                Price = 799m,
                StockQuantity = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                CategoryId = 2,
                Name = "Test Keyboard",
                SKU = "LIST-KEYBOARD-001",
                Price = 1499m,
                StockQuantity = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.SKU == "LIST-MOUSE-001");
        Assert.Contains(result, p => p.SKU == "LIST-KEYBOARD-001");
    }

    [Fact]
    public async Task RestoreAsync_NonExistingProduct_ReturnsNull()
    {
        var (_, service) = CreateService();

        var result = await service.RestoreAsync(
            9999,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_DeletedProduct_ExcludesDeletedProduct()
    {
        var (context, service) = CreateService();

        context.Products.AddRange(
            new Product
            {
                CategoryId = 2,
                Name = "Active Mouse",
                SKU = "ACTIVE-MOUSE-001",
                Price = 799m,
                StockQuantity = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                CategoryId = 2,
                Name = "Deleted Keyboard",
                SKU = "DELETED-KEYBOARD-001",
                Price = 1499m,
                StockQuantity = 5,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("ACTIVE-MOUSE-001", result[0].SKU);
    }

}
