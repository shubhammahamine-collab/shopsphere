using FluentValidation.TestHelper;
using ShopSphere.Application.Features.Orders.Requests;
using ShopSphere.Application.Features.Orders.Validators;

namespace ShopSphere.UnitTests.Orders;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    private static CreateOrderRequest CreateValidRequest()
    {
        return new CreateOrderRequest
        {
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = 1,
                    Quantity = 2
                }
            ]
        };
    }

    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(CreateValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullItems_HasError()
    {
        var request = new CreateOrderRequest
        {
            Items = null!
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Order items are required.");
    }

    [Fact]
    public void Validate_EmptyItems_HasError()
    {
        var request = new CreateOrderRequest
        {
            Items = []
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("An order must contain at least one item.");
    }

    [Fact]
    public void Validate_InvalidProductId_HasError()
    {
        var request = CreateValidRequest();
        request.Items[0].ProductId = 0;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].ProductId")
            .WithErrorMessage("Product ID must be greater than 0.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidQuantity_HasError(int quantity)
    {
        var request = CreateValidRequest();
        request.Items[0].Quantity = quantity;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("Order quantity must be greater than 0.");
    }

    [Fact]
    public void Validate_MultipleValidItems_HasNoErrors()
    {
        var request = new CreateOrderRequest
        {
            Items =
            [
                new CreateOrderItemRequest { ProductId = 1, Quantity = 2 },
                new CreateOrderItemRequest { ProductId = 2, Quantity = 5 }
            ]
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
