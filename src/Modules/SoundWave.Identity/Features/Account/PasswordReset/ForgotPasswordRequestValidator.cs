using FluentValidation;

namespace SoundWave.Identity.Features.Account.PasswordReset;

/// <summary>
/// Validator for format and shape of <see cref="ForgotPasswordRequest"/>.
/// </summary>
internal class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordRequestValidator"/> class.
    /// </summary>
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}
