using FluentValidation.TestHelper;
using ShopSphere.Application.Features.Products.Requests;
using ShopSphere.Application.Features.Products.Validators;

namespace ShopSphere.UnitTests.Products;

public class UpdateProductRequestValidatorTests
{
    private readonly UpdateProductRequestValidator _validator = new();

    private static UpdateProductRequest CreateValidRequest()
    {
        return new UpdateProductRequest
        {
            CategoryId = 1,
            Name = "Wireless Mouse",
            Description = "A wireless mouse",
            SKU = "WM-001",
            Price = 799
        };
    }

    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(CreateValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidCategoryId_HasError(int categoryId)
    {
        var request = CreateValidRequest();
        request.CategoryId = categoryId;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorMessage("Category ID must be greater than 0.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyOrWhitespaceName_HasError(string name)
    {
        var request = CreateValidRequest();
        request.Name = name;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name is required.");
    }

    [Fact]
    public void Validate_NameExceeds200Characters_HasError()
    {
        var request = CreateValidRequest();
        request.Name = new string('A', 201);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_NameExactly200Characters_IsValid()
    {
        var request = CreateValidRequest();
        request.Name = new string('A', 200);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_DescriptionExceeds2000Characters_HasError()
    {
        var request = CreateValidRequest();
        request.Description = new string('A', 2001);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Product description cannot exceed 2000 characters.");
    }

    [Fact]
    public void Validate_DescriptionExactly2000Characters_IsValid()
    {
        var request = CreateValidRequest();
        request.Description = new string('A', 2000);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NullDescription_IsValid()
    {
        var request = CreateValidRequest();
        request.Description = null;

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_EmptySKU_HasError()
    {
        var request = CreateValidRequest();
        request.SKU = "";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SKU)
            .WithErrorMessage("SKU is required.");
    }

    [Fact]
    public void Validate_SKUExceeds50Characters_HasError()
    {
        var request = CreateValidRequest();
        request.SKU = new string('A', 51);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SKU)
            .WithErrorMessage("SKU cannot exceed 50 characters.");
    }

    [Fact]
    public void Validate_SKUExactly50Characters_IsValid()
    {
        var request = CreateValidRequest();
        request.SKU = new string('A', 50);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.SKU);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidPrice_HasError(decimal price)
    {
        var request = CreateValidRequest();
        request.Price = price;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be greater than 0.");
    }
}
