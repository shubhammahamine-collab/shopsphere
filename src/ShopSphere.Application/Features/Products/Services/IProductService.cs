using ShopSphere.Application.Features.Products.DTOs;
using ShopSphere.Application.Features.Products.Requests;

namespace ShopSphere.Application.Features.Products.Services;

public interface IProductService
{
    Task<ProductDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<ProductDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<ProductDto?> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken);

    Task<ProductDto?> RestoreAsync(
        int id,
        CancellationToken cancellationToken);
}