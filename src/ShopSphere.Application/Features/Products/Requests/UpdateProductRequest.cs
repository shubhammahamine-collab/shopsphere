namespace ShopSphere.Application.Features.Products.Requests;

public class UpdateProductRequest
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string SKU { get; set; } = string.Empty;

    public decimal Price { get; set; }
}