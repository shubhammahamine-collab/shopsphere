using FluentValidation.TestHelper;
using ShopSphere.Application.Features.Users.Requests;
using ShopSphere.Application.Features.Users.Validators;

namespace ShopSphere.UnitTests.Users;

public class LoginUserRequestValidatorTests
{
    private readonly LoginUserRequestValidator _validator = new();

    private static LoginUserRequest CreateValidRequest()
    {
        return new LoginUserRequest
        {
            Email = "rahul@example.com",
            Password = "Password123"
        };
    }

    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(CreateValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyEmail_HasError(string email)
    {
        var request = CreateValidRequest();
        request.Email = email;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("user@")]
    public void Validate_InvalidEmail_HasError(string email)
    {
        var request = CreateValidRequest();
        request.Email = email;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("A valid email address is required.");
    }

    [Fact]
    public void Validate_EmailExceeds256Characters_HasError()
    {
        var request = CreateValidRequest();
        request.Email = $"{new string('a', 245)}@example.com";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email cannot exceed 256 characters.");
    }

    [Fact]
    public void Validate_EmailExactly256Characters_IsValid()
    {
        var request = CreateValidRequest();
        request.Email = $"{new string('a', 244)}@example.com";

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_EmptyPassword_HasError()
    {
        var request = CreateValidRequest();
        request.Password = "";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Fact]
    public void Validate_SingleCharacterPassword_IsValid()
    {
        var request = CreateValidRequest();
        request.Password = "x";

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
