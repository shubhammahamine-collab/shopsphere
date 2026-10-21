using FluentValidation.TestHelper;
using ShopSphere.Application.Features.Users.Requests;
using ShopSphere.Application.Features.Users.Validators;

namespace ShopSphere.UnitTests.Users;

public class RegisterUserRequestValidatorTests
{
    private readonly RegisterUserRequestValidator _validator = new();

    private static RegisterUserRequest CreateValidRequest()
    {
        return new RegisterUserRequest
        {
            FirstName = "Rahul",
            LastName = "Patil",
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
    public void Validate_EmptyFirstName_HasError(string firstName)
    {
        var request = CreateValidRequest();
        request.FirstName = firstName;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name is required.");
    }

    [Fact]
    public void Validate_FirstNameExceeds100Characters_HasError()
    {
        var request = CreateValidRequest();
        request.FirstName = new string('A', 101);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_FirstNameExactly100Characters_IsValid()
    {
        var request = CreateValidRequest();
        request.FirstName = new string('A', 100);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyLastName_HasError(string lastName)
    {
        var request = CreateValidRequest();
        request.LastName = lastName;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name is required.");
    }

    [Fact]
    public void Validate_LastNameExceeds100Characters_HasError()
    {
        var request = CreateValidRequest();
        request.LastName = new string('A', 101);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_LastNameExactly100Characters_IsValid()
    {
        var request = CreateValidRequest();
        request.LastName = new string('A', 100);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_EmptyEmail_HasError()
    {
        var request = CreateValidRequest();
        request.Email = "";

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
    public void Validate_PasswordEmpty_HasError()
    {
        var request = CreateValidRequest();
        request.Password = "";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Fact]
    public void Validate_PasswordShorterThan8Characters_HasError()
    {
        var request = CreateValidRequest();
        request.Password = "Abc1234";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters long.");
    }

    [Fact]
    public void Validate_PasswordExactly8Characters_IsValid()
    {
        var request = CreateValidRequest();
        request.Password = "Abc12345";

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordExceeds128Characters_HasError()
    {
        var request = CreateValidRequest();
        request.Password = new string('A', 129);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password cannot exceed 128 characters.");
    }

    [Fact]
    public void Validate_PasswordExactly128Characters_IsValid()
    {
        var request = CreateValidRequest();
        request.Password = new string('A', 128);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
