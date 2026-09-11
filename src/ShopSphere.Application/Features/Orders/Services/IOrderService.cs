using ShopSphere.Application.Features.Orders.DTOs;
using ShopSphere.Application.Features.Orders.Requests;

namespace ShopSphere.Application.Features.Orders.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request);

    Task<List<OrderDto>> GetAllAsync();

    Task<OrderDto?> GetByIdAsync(int id);

    Task<OrderDto?> UpdateStatusAsync(int id, string status);
}