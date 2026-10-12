using FluentValidation;
using ShopSphere.Application.Features.Products.Requests;

namespace ShopSphere.Application.Features.Products.Validators;

public class UpdateStockRequestValidator
    : AbstractValidator<UpdateStockRequest>
{
    public UpdateStockRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity cannot be negative.");
    }
}