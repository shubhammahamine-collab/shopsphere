namespace ShopSphere.Application.Features.Products.Requests;

public class ProductQueryRequest
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? SearchTerm { get; set; }

    public int? CategoryId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string SortBy { get; set; } = "Name";

    public bool SortDescending { get; set; } = false;
}
