using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<OrderService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<OrderDto> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating order for user {UserId} with {ItemCount} items.",
            _currentUserService.UserId,
            request.Items.Count);

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
                .FirstOrDefaultAsync(
                    x => x.Id == item.ProductId,
                    cancellationToken);

            if (product == null)
            {
                _logger.LogWarning(
                    "Order creation failed because product {ProductId} was not found.",
                    item.ProductId);

                throw new KeyNotFoundException(
                    $"Product with ID {item.ProductId} was not found.");
            }

            if (item.Quantity <= 0)
            {
                _logger.LogWarning(
                    "Invalid order quantity {Quantity} for product {ProductId}.",
                    item.Quantity,
                    item.ProductId);

                throw new ArgumentException(
                    $"Quantity for product {item.ProductId} must be greater than 0.");
            }

            if (product.StockQuantity < item.Quantity)
            {
                _logger.LogWarning(
                    "Insufficient stock for product {ProductId}. Available: {AvailableStock}, Requested: {RequestedQuantity}.",
                    product.Id,
                    product.StockQuantity,
                    item.Quantity);

                throw new InvalidOperationException(
                    $"Insufficient stock for product {product.Name}. " +
                    $"Available: {product.StockQuantity}, Requested: {item.Quantity}.");
            }

            var totalPrice = product.Price * item.Quantity;

            product.StockQuantity -= item.Quantity;

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

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderNumber} created successfully for user {UserId} with total amount {TotalAmount}.",
            order.OrderNumber,
            order.UserId,
            order.TotalAmount);

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

    public async Task<List<OrderDto>> GetAllAsync(
        CancellationToken cancellationToken)
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
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
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

        var order = await query.FirstOrDefaultAsync(
            cancellationToken);

        return order == null
            ? null
            : MapToDto(order);
    }

    public async Task<OrderDto?> UpdateStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(x => x.OrderItems)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

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

        var oldStatus = order.Status;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} status changed from {OldStatus} to {NewStatus} by user {UserId}.",
            order.Id,
            oldStatus,
            status,
            _currentUserService.UserId);

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