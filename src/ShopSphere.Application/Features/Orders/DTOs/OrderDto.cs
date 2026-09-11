namespace ShopSphere.Application.Features.Orders.DTOs;

public class OrderDto
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<OrderItemDto> Items { get; set; } = new();
}