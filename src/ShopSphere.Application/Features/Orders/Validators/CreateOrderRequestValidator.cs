using FluentValidation;
using ShopSphere.Application.Features.Orders.Requests;

namespace ShopSphere.Application.Features.Orders.Validators;

public class CreateOrderRequestValidator
: AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.Items)
        .NotNull()
        .WithMessage("Order items are required.")
        .NotEmpty()
        .WithMessage("An order must contain at least one item.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemRequestValidator());
    }
}

public class CreateOrderItemRequestValidator
: AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
        .GreaterThan(0)
        .WithMessage("Product ID must be greater than 0.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Order quantity must be greater than 0.");
    }
}
