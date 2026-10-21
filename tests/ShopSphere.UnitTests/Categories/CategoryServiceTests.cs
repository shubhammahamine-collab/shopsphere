using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShopSphere.Application.Features.Categories.Requests;
using ShopSphere.Application.Features.Categories.Services;
using ShopSphere.Domain.Entities;

namespace ShopSphere.UnitTests.Categories;

public class CategoryServiceTests
{
    private static (
        ShopSphereDbContextStub Context,
        CategoryService Service) CreateService()
    {
        var options = new DbContextOptionsBuilder<ShopSphereDbContextStub>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ShopSphereDbContextStub(options);

        var logger = Mock.Of<ILogger<CategoryService>>();
        var service = new CategoryService(context, logger);

        return (context, service);
    }

    private static Category CreateCategory(
        string name = "Electronics",
        string? description = "Electronic products",
        bool isActive = true)
    {
        return new Category
        {
            Name = name,
            Description = description,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesCategory()
    {
        var (context, service) = CreateService();
        var request = new CreateCategoryRequest
        {
            Name = "Electronics",
            Description = "Electronic products"
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.Equal("Electronics", result.Name);
        Assert.Equal("Electronic products", result.Description);

        var saved = await context.Categories
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == result.Id);

        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCategories()
    {
        var (context, service) = CreateService();

        context.Categories.AddRange(
            CreateCategory("Electronics"),
            CreateCategory("Books"));

        await context.SaveChangesAsync();

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Name == "Electronics");
        Assert.Contains(result, x => x.Name == "Books");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCategory_ReturnsCategory()
    {
        var (context, service) = CreateService();
        var category = CreateCategory();

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var result = await service.GetByIdAsync(
            category.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Electronics", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_MissingCategory_ReturnsNull()
    {
        var (_, service) = CreateService();

        var result = await service.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ExistingCategory_UpdatesFields()
    {
        var (context, service) = CreateService();
        var category = CreateCategory();

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var request = new UpdateCategoryRequest
        {
            Name = "Updated Electronics",
            Description = "Updated description"
        };

        var result = await service.UpdateAsync(
            category.Id, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated Electronics", result.Name);
        Assert.Equal("Updated description", result.Description);

        var saved = await context.Categories.FindAsync(category.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_MissingCategory_ReturnsNull()
    {
        var (_, service) = CreateService();
        var request = new UpdateCategoryRequest
        {
            Name = "Updated",
            Description = "Updated description"
        };

        var result = await service.UpdateAsync(
            999, request, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingCategory_SoftDeletesCategory()
    {
        var (context, service) = CreateService();
        var category = CreateCategory();

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var result = await service.DeleteAsync(
            category.Id, CancellationToken.None);

        Assert.True(result);

        var saved = await context.Categories
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == category.Id);

        Assert.False(saved.IsActive);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_MissingCategory_ReturnsFalse()
    {
        var (_, service) = CreateService();

        var result = await service.DeleteAsync(999, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_DeletedCategory_RestoresCategory()
    {
        var (context, service) = CreateService();
        var category = CreateCategory(isActive: false);

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var result = await service.RestoreAsync(
            category.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);

        var saved = await context.Categories
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == category.Id);

        Assert.True(saved.IsActive);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task RestoreAsync_MissingCategory_ReturnsNull()
    {
        var (_, service) = CreateService();

        var result = await service.RestoreAsync(999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RestoreAsync_AlreadyActiveCategory_ReturnsCategory()
    {
        var (context, service) = CreateService();
        var category = CreateCategory();

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var result = await service.RestoreAsync(
            category.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Electronics", result.Name);
    }
}
