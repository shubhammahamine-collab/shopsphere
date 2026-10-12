using FluentValidation;
using ShopSphere.Application.Features.Products.Requests;

namespace ShopSphere.Application.Features.Products.Validators;

public class UpdateProductRequestValidator
    : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Category ID must be greater than 0.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Product description cannot exceed 2000 characters.");

        RuleFor(x => x.SKU)
            .NotEmpty()
            .WithMessage("SKU is required.")
            .MaximumLength(50)
            .WithMessage("SKU cannot exceed 50 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0.");
    }
}