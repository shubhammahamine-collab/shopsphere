using FluentValidation.TestHelper;
using ShopSphere.Application.Features.Products.Requests;
using ShopSphere.Application.Features.Products.Validators;

namespace ShopSphere.UnitTests.Products;

public class UpdateStockRequestValidatorTests
{
    private readonly UpdateStockRequestValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1000)]
    public void Validate_NonNegativeQuantity_HasNoErrors(int quantity)
    {
        var request = new UpdateStockRequest
        {
            Quantity = quantity
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_NegativeQuantity_HasError(int quantity)
    {
        var request = new UpdateStockRequest
        {
            Quantity = quantity
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Stock quantity cannot be negative.");
    }
}
