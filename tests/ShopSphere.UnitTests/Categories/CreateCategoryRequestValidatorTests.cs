using FluentValidation.TestHelper;
using ShopSphere.Application.Features.Categories.Requests;
using ShopSphere.Application.Features.Categories.Validators;

namespace ShopSphere.UnitTests.Categories;

public class CreateCategoryRequestValidatorTests
{
    private readonly CreateCategoryRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Electronics",
            Description = "Electronic products"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyName_HasValidationError(string name)
    {
        var request = new CreateCategoryRequest
        {
            Name = name,
            Description = "Electronic products"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name is required.");
    }

    [Fact]
    public void Validate_NameExceeds100Characters_HasValidationError()
    {
        var request = new CreateCategoryRequest
        {
            Name = new string('A', 101),
            Description = "Electronic products"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_NameExactly100Characters_HasNoNameError()
    {
        var request = new CreateCategoryRequest
        {
            Name = new string('A', 100),
            Description = "Electronic products"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_DescriptionExceeds500Characters_HasValidationError()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Electronics",
            Description = new string('D', 501)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage(
                "Category description cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_DescriptionExactly500Characters_HasNoDescriptionError()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Electronics",
            Description = new string('D', 500)
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NullDescription_HasNoDescriptionError()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Electronics",
            Description = null
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
}
