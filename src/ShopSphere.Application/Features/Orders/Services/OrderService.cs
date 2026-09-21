using Microsoft.EntityFrameworkCore;
using ShopSphere.Application.Abstractions;
using ShopSphere.Application.Features.Orders.DTOs;
using ShopSphere.Application.Features.Orders.Requests;
using ShopSphere.Domain.Common;
using ShopSphere.Domain.Entities;

namespace ShopSphere.Application.Features.Orders.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public OrderService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            UserId = _currentUserService.UserId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}"[..12].ToUpper(),
            OrderDate = DateTime.UtcNow,
            Status = OrderStatuses.Pending,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        decimal totalAmount = 0;

        foreach (var item in request.Items)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == item.ProductId);

            if (product == null)
            {
                throw new Exception(
                    $"Product with ID {item.ProductId} was not found.");
            }

            if (item.Quantity <= 0)
            {
                throw new Exception(
                    $"Quantity for product {item.ProductId} must be greater than 0.");
            }

            var totalPrice = product.Price * item.Quantity;

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                TotalPrice = totalPrice,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            order.OrderItems.Add(orderItem);

            totalAmount += totalPrice;
        }

        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);

        await _context.SaveChangesAsync();

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,

            Items = order.OrderItems
                .Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                })
                .ToList()
        };
    }

    public async Task<List<OrderDto>> GetAllAsync()
    {
        var query = _context.Orders.AsNoTracking();

        if (!_currentUserService.IsAdmin)
        {
            query = query.Where(x =>
                x.UserId == _currentUserService.UserId);
        }

        return await query
            .Select(x => new OrderDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                OrderDate = x.OrderDate,
                TotalAmount = x.TotalAmount,
                Status = x.Status
            })
            .ToListAsync();
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(x => x.OrderItems)
            .Where(x => x.Id == id);

        if (!_currentUserService.IsAdmin)
        {
            query = query.Where(x =>
                x.UserId == _currentUserService.UserId);
        }

        var order = await query.FirstOrDefaultAsync();

        return order == null
            ? null
            : MapToDto(order);
    }

    public async Task<OrderDto?> UpdateStatusAsync(int id, string status)
    {
        var order = await _context.Orders
            .Include(x => x.OrderItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order == null)
        {
            return null;
        }

        ValidateStatus(status);

        if (order.Status == status)
        {
            return MapToDto(order);
        }

        ValidateStatusTransition(order.Status, status);

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(order);
    }

    private void ValidateStatus(string status)
    {
        var validStatuses = new[]
        {
            OrderStatuses.Pending,
            OrderStatuses.Confirmed,
            OrderStatuses.Shipped,
            OrderStatuses.Delivered,
            OrderStatuses.Cancelled
        };

        if (!validStatuses.Contains(status))
        {
            throw new ArgumentException($"Invalid order status: {status}");
        }
    }

    private void ValidateStatusTransition(
        string currentStatus,
        string newStatus)
    {
        if (currentStatus == OrderStatuses.Pending &&
            newStatus != OrderStatuses.Confirmed &&
            newStatus != OrderStatuses.Cancelled)
        {
            throw new InvalidOperationException(
                "Pending order can only be Confirmed or Cancelled.");
        }

        if (currentStatus == OrderStatuses.Confirmed &&
            newStatus != OrderStatuses.Shipped &&
            newStatus != OrderStatuses.Cancelled)
        {
            throw new InvalidOperationException(
                "Confirmed order can only be Shipped or Cancelled.");
        }

        if (currentStatus == OrderStatuses.Shipped &&
            newStatus != OrderStatuses.Delivered)
        {
            throw new InvalidOperationException(
                "Shipped order can only be Delivered.");
        }

        if ((currentStatus == OrderStatuses.Delivered ||
             currentStatus == OrderStatuses.Cancelled) &&
            newStatus != currentStatus)
        {
            throw new InvalidOperationException(
                $"{currentStatus} order cannot be changed.");
        }
    }

    private OrderDto MapToDto(Order order)
    {
        // convert Order → OrderDto
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,

            Items = order.OrderItems
                    .Select(item => new OrderItemDto
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
        };
    }
}