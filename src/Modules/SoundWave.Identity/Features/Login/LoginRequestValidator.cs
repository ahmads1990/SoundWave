using FluentValidation;

namespace SoundWave.Identity.Features.Login;

/// <summary>
/// Validator for format and shape of <see cref="LoginRequest"/>.
/// </summary>
internal class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginRequestValidator"/> class.
    /// Defines validation rules for Email and Password fields.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
