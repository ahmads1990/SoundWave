using FluentValidation;

namespace SoundWave.Identity.Features.ResendVerificationEmail;

/// <summary>
/// Validates the resend verification email request.
/// </summary>
public class ResendVerificationEmailRequestValidator : AbstractValidator<ResendVerificationEmailRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResendVerificationEmailRequestValidator"/> class.
    /// </summary>
    public ResendVerificationEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.")
            .EmailAddress().WithMessage("A valid email is required.");
    }
}
