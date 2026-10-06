using ShopSphere.Application.Features.Orders.DTOs;
using ShopSphere.Application.Features.Orders.Requests;

namespace ShopSphere.Application.Features.Orders.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken);

    Task<List<OrderDto>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<OrderDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<OrderDto?> UpdateStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken);
}