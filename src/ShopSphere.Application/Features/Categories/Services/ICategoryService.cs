using ShopSphere.Application.Features.Categories.DTOs;
using ShopSphere.Application.Features.Categories.Requests;

namespace ShopSphere.Application.Features.Categories.Services;

public interface ICategoryService
{
    Task<CategoryDto> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<CategoryDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<CategoryDto?> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken);

    Task<CategoryDto?> RestoreAsync(
        int id,
        CancellationToken cancellationToken);
}