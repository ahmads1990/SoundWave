using FluentValidation;

namespace SoundWave.Identity.Features.Register;

/// <summary>
/// Provides validation rules for the <see cref="RegisterCommand"/>, specifically ensuring password complexity requirements.
/// </summary>
internal class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    /// <summary>
    /// Initializes validation rules for the registration command.
    /// </summary>
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Password)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
    }
}
