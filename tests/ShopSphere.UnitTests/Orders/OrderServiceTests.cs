using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShopSphere.Application.Abstractions;
using ShopSphere.Application.Features.Orders.Requests;
using ShopSphere.Application.Features.Orders.Services;
using ShopSphere.Domain.Common;
using ShopSphere.Domain.Entities;

namespace ShopSphere.UnitTests.Orders;

public class OrderServiceTests
{
    private static (
        ShopSphereDbContextStub Context,
        OrderService Service)
        CreateService(int userId = 1, bool isAdmin = false)
    {
        var options =
            new DbContextOptionsBuilder<ShopSphereDbContextStub>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        var context = new ShopSphereDbContextStub(options);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);
        currentUser.Setup(x => x.IsAdmin).Returns(isAdmin);
        currentUser.Setup(x => x.Role)
            .Returns(isAdmin ? UserRoles.Admin : UserRoles.Customer);

        var logger = Mock.Of<ILogger<OrderService>>();

        var service = new OrderService(
            context,
            currentUser.Object,
            logger);

        return (context, service);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesOrderAndDeductsStock()
    {
        var (context, service) = CreateService(userId: 7);

        var product = new Product
        {
            CategoryId = 2,
            Name = "Test Mouse",
            SKU = "ORDER-MOUSE-001",
            Price = 100m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 2
                }
            }
        };

        var result = await service.CreateAsync(
            request,
            CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.StartsWith("ORD-", result.OrderNumber);
        Assert.Equal(OrderStatuses.Pending, result.Status);
        Assert.Equal(200m, result.TotalAmount);
        Assert.Single(result.Items);
        Assert.Equal(product.Id, result.Items[0].ProductId);
        Assert.Equal(2, result.Items[0].Quantity);
        Assert.Equal(100m, result.Items[0].UnitPrice);
        Assert.Equal(200m, result.Items[0].TotalPrice);

        var updatedProduct = await context.Products
            .FirstAsync(p => p.Id == product.Id);

        Assert.Equal(8, updatedProduct.StockQuantity);

        var savedOrder = await context.Orders
            .Include(o => o.OrderItems)
            .FirstAsync(o => o.Id == result.Id);

        Assert.Equal(7, savedOrder.UserId);
        Assert.Equal(200m, savedOrder.TotalAmount);
        Assert.Single(savedOrder.OrderItems);
    }

    [Fact]
    public async Task CreateAsync_NonExistingProduct_ThrowsKeyNotFoundException()
    {
        var (_, service) = CreateService();

        var request = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 9999,
                    Quantity = 1
                }
            }
        };

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(
                request,
                CancellationToken.None));

        Assert.Equal(
            "Product with ID 9999 was not found.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_InvalidQuantity_ThrowsArgumentException(
        int quantity)
    {
        var (context, service) = CreateService();

        var product = new Product
        {
            CategoryId = 2,
            Name = "Quantity Test Mouse",
            SKU = $"QUANTITY-MOUSE-{Guid.NewGuid():N}",
            Price = 100m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
        {
            new()
            {
                ProductId = product.Id,
                Quantity = quantity
            }
        }
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                request,
                CancellationToken.None));

        Assert.Equal(
            $"Quantity for product {product.Id} must be greater than 0.",
            exception.Message);

        Assert.Empty(await context.Orders.ToListAsync());

        var unchangedProduct = await context.Products
            .FirstAsync(p => p.Id == product.Id);

        Assert.Equal(10, unchangedProduct.StockQuantity);
    }

    [Fact]
    public async Task CreateAsync_InsufficientStock_ThrowsInvalidOperationException()
    {
        var (context, service) = CreateService();

        var product = new Product
        {
            CategoryId = 2,
            Name = "Limited Stock Mouse",
            SKU = "LIMITED-MOUSE-001",
            Price = 100m,
            StockQuantity = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
        {
            new()
            {
                ProductId = product.Id,
                Quantity = 5
            }
        }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(
                request,
                CancellationToken.None));

        Assert.Equal(
            "Insufficient stock for product Limited Stock Mouse. Available: 3, Requested: 5.",
            exception.Message);

        Assert.Empty(await context.Orders.ToListAsync());

        var unchangedProduct = await context.Products
            .FirstAsync(p => p.Id == product.Id);

        Assert.Equal(3, unchangedProduct.StockQuantity);
    }

    [Fact]
    public async Task CreateAsync_SecondItemInsufficientStock_DoesNotPersistChanges()
    {
        var (context, service) = CreateService();

        var firstProduct = new Product
        {
            CategoryId = 2,
            Name = "Available Mouse",
            SKU = "AVAILABLE-MOUSE-001",
            Price = 100m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var secondProduct = new Product
        {
            CategoryId = 2,
            Name = "Limited Keyboard",
            SKU = "LIMITED-KEYBOARD-001",
            Price = 200m,
            StockQuantity = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.AddRange(firstProduct, secondProduct);
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
        {
            new()
            {
                ProductId = firstProduct.Id,
                Quantity = 2
            },
            new()
            {
                ProductId = secondProduct.Id,
                Quantity = 3
            }
        }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request, CancellationToken.None));

        Assert.Empty(await context.Orders.ToListAsync());

        // Reload entities to verify what was persisted to the database.
        await context.Entry(firstProduct).ReloadAsync();
        await context.Entry(secondProduct).ReloadAsync();

        Assert.Equal(10, firstProduct.StockQuantity);
        Assert.Equal(1, secondProduct.StockQuantity);
    }

    [Fact]
    public async Task GetByIdAsync_OwnOrder_ReturnsOrder()
    {
        var (context, service) = CreateService(userId: 7);

        var order = new Order
        {
            UserId = 7,
            OrderNumber = "ORD-OWN-00001",
            OrderDate = DateTime.UtcNow,
            TotalAmount = 200m,
            Status = OrderStatuses.Pending,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var result = await service.GetByIdAsync(
            order.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("ORD-OWN-00001", result.OrderNumber);
    }

    [Fact]
    public async Task GetByIdAsync_OtherUsersOrder_ReturnsNull()
    {
        var (context, service) = CreateService(userId: 7);

        var order = new Order
        {
            UserId = 99,
            OrderNumber = "ORD-OTHER-0001",
            OrderDate = DateTime.UtcNow,
            TotalAmount = 200m,
            Status = OrderStatuses.Pending,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var result = await service.GetByIdAsync(
            order.Id,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_AdminCanRetrieveAnotherUsersOrder()
    {
        var (context, service) = CreateService(
            userId: 7,
            isAdmin: true);

        var order = new Order
        {
            UserId = 99,
            OrderNumber = "ORD-ADMIN-0001",
            OrderDate = DateTime.UtcNow,
            TotalAmount = 350m,
            Status = OrderStatuses.Pending,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var result = await service.GetByIdAsync(
            order.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("ORD-ADMIN-0001", result.OrderNumber);
    }

    [Fact]
    public async Task GetAllAsync_Customer_ReturnsOnlyOwnOrders()
    {
        var (context, service) = CreateService(userId: 7);

        context.Orders.AddRange(
            new Order
            {
                UserId = 7,
                OrderNumber = "ORD-CUST-0001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 200m,
                Status = OrderStatuses.Pending
            },
            new Order
            {
                UserId = 99,
                OrderNumber = "ORD-CUST-0002",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 300m,
                Status = OrderStatuses.Confirmed
            },
            new Order
            {
                UserId = 7,
                OrderNumber = "ORD-CUST-0003",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 400m,
                Status = OrderStatuses.Shipped
            });

        await context.SaveChangesAsync();

        var result = await service.GetAllAsync(
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.OrderNumber == "ORD-CUST-0001");
        Assert.Contains(result, x => x.OrderNumber == "ORD-CUST-0003");
        Assert.DoesNotContain(result, x => x.OrderNumber == "ORD-CUST-0002");
    }

    [Fact]
    public async Task GetAllAsync_Admin_ReturnsAllUsersOrders()
    {
        var (context, service) = CreateService(
            userId: 7,
            isAdmin: true);

        context.Orders.AddRange(
            new Order
            {
                UserId = 7,
                OrderNumber = "ORD-ADMIN-0001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 200m,
                Status = OrderStatuses.Pending
            },
            new Order
            {
                UserId = 99,
                OrderNumber = "ORD-ADMIN-0002",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 500m,
                Status = OrderStatuses.Confirmed
            });

        await context.SaveChangesAsync();

        var result = await service.GetAllAsync(
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.OrderNumber == "ORD-ADMIN-0001");
        Assert.Contains(result, x => x.OrderNumber == "ORD-ADMIN-0002");
    }

    [Fact]
    public async Task GetAllAsync_NoOrders_ReturnsEmptyList()
    {
        var (_, service) = CreateService(userId: 7);

        var result = await service.GetAllAsync(
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingOrder_ReturnsNull()
    {
        var (_, service) = CreateService(userId: 7);

        var result = await service.GetByIdAsync(
            99999,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Pending", "Confirmed")]
    [InlineData("Pending", "Cancelled")]
    [InlineData("Confirmed", "Shipped")]
    [InlineData("Confirmed", "Cancelled")]
    [InlineData("Shipped", "Delivered")]
    public async Task UpdateStatusAsync_ValidTransition_UpdatesStatus(
        string currentStatus,
        string newStatus)
    {
        var (context, service) = CreateService(
            userId: 7,
            isAdmin: true);

        var order = new Order
        {
            UserId = 99,
            OrderNumber = "ORD-STATUS-0001",
            OrderDate = DateTime.UtcNow,
            TotalAmount = 500m,
            Status = currentStatus,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var result = await service.UpdateStatusAsync(
            order.Id,
            newStatus,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(newStatus, result.Status);
        Assert.Equal(newStatus, order.Status);
        Assert.NotNull(order.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        var (context, service) = CreateService(
            userId: 7,
            isAdmin: true);

        var order = new Order
        {
            UserId = 7,
            OrderNumber = "ORD-INVALID-0001",
            OrderDate = DateTime.UtcNow,
            TotalAmount = 100m,
            Status = OrderStatuses.Pending,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateStatusAsync(
                order.Id,
                "InvalidStatus",
                CancellationToken.None));

        Assert.Equal(OrderStatuses.Pending, order.Status);
    }

    [Theory]
    [InlineData("Pending", "Shipped")]
    [InlineData("Pending", "Delivered")]
    [InlineData("Confirmed", "Delivered")]
    [InlineData("Shipped", "Cancelled")]
    [InlineData("Delivered", "Cancelled")]
    [InlineData("Cancelled", "Confirmed")]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsInvalidOperationException(
        string currentStatus,
        string newStatus)
    {
        var (context, service) = CreateService(
            userId: 7,
            isAdmin: true);

        var order = new Order
        {
            UserId = 7,
            OrderNumber = "ORD-INVALID-TRANSITION",
            OrderDate = DateTime.UtcNow,
            TotalAmount = 250m,
            Status = currentStatus,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStatusAsync(
                order.Id,
                newStatus,
                CancellationToken.None));

        Assert.Equal(currentStatus, order.Status);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Confirmed")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public async Task UpdateStatusAsync_SameStatus_ReturnsOrderWithoutChangingStatus(
        string status)
    {
        var (context, service) = CreateService(
            userId: 7,
            isAdmin: true);

        var order = new Order
        {
            UserId = 99,
            OrderNumber = "ORD-SAME-STATUS",
            OrderDate = DateTime.UtcNow,
            TotalAmount = 150m,
            Status = status,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var result = await service.UpdateStatusAsync(
            order.Id,
            status,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(status, result.Status);
        Assert.Null(order.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistingOrder_ReturnsNull()
    {
        var (_, service) = CreateService(
            userId: 7,
            isAdmin: true);

        var result = await service.UpdateStatusAsync(
            99999,
            OrderStatuses.Confirmed,
            CancellationToken.None);

        Assert.Null(result);
    }

}
