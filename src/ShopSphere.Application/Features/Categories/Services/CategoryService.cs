using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopSphere.Application.Abstractions;
using ShopSphere.Application.Features.Categories.DTOs;
using ShopSphere.Application.Features.Categories.Requests;
using ShopSphere.Domain.Entities;

namespace ShopSphere.Application.Features.Categories.Services;

public class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        IApplicationDbContext context,
        ILogger<CategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating category {CategoryName}.",
            request.Name);

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Category {CategoryId} created successfully with name {CategoryName}.",
            category.Id,
            category.Name);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AsNoTracking()
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CategoryDto?> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        if (category is null)
        {
            _logger.LogWarning(
                "Category {CategoryId} was not found for update.",
                id);

            return null;
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Category {CategoryId} updated successfully.",
            category.Id);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        if (category is null)
        {
            _logger.LogWarning(
                "Category {CategoryId} was not found for deletion.",
                id);

            return false;
        }

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Category {CategoryId} soft deleted successfully.",
            category.Id);

        return true;
    }

    public async Task<CategoryDto?> RestoreAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        if (category is null)
        {
            _logger.LogWarning(
                "Category {CategoryId} was not found for restore.",
                id);

            return null;
        }

        if (category.IsActive)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }

        category.IsActive = true;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Category {CategoryId} restored successfully.",
            category.Id);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }
}