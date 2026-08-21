using FluentValidation;

namespace SoundWave.Identity.Features.Account.VerifyEmail;

/// <summary>
/// Validates the verify email request.
/// </summary>
public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyEmailRequestValidator"/> class.
    /// </summary>
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP is required.")
            .Length(6).WithMessage("OTP must be exactly 6 characters.");
    }
}
