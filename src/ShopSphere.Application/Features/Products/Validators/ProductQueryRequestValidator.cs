using FluentValidation;
using ShopSphere.Application.Features.Products.Requests;

namespace ShopSphere.Application.Features.Products.Validators;

public class ProductQueryRequestValidator
    : AbstractValidator<ProductQueryRequest>
{
    private static readonly string[] AllowedSortFields =
    [
        "Name",
        "Price",
        "SKU",
        "StockQuantity"
    ];

    public ProductQueryRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .WithMessage("Search term cannot exceed 100 characters.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .When(x => x.CategoryId.HasValue)
            .WithMessage("Category ID must be greater than 0.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue)
            .WithMessage("Minimum price cannot be negative.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("Maximum price cannot be negative.");

        RuleFor(x => x)
            .Must(x =>
                !x.MinPrice.HasValue ||
                !x.MaxPrice.HasValue ||
                x.MaxPrice.Value >= x.MinPrice.Value)
            .WithMessage("Maximum price must be greater than or equal to minimum price.");

        RuleFor(x => x.SortBy)
            .Must(sortBy =>
                !string.IsNullOrWhiteSpace(sortBy) &&
                AllowedSortFields.Contains(
                    sortBy,
                    StringComparer.OrdinalIgnoreCase))
            .WithMessage(
                "Sort field must be Name, Price, SKU, or StockQuantity.");
    }
}
