using FluentValidation;
using ShopSphere.Application.Features.Users.Requests;

namespace ShopSphere.Application.Features.Users.Validators;

public class LoginUserRequestValidator
: AbstractValidator<LoginUserRequest>
{
    public LoginUserRequestValidator()
    {
        RuleFor(x => x.Email)
        .NotEmpty()
        .WithMessage("Email is required.")
        .EmailAddress()
        .WithMessage("A valid email address is required.")
        .MaximumLength(256)
        .WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}
