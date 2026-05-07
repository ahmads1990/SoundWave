using FluentValidation;

namespace SoundWave.Identity.Features.Register;

/// <summary>
/// Provides validation rules for the <see cref="RegisterRequest"/> to ensure schema and basic field constraints are met.
/// </summary>
internal class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>
    /// Initializes validation rules for the registration request.
    /// </summary>
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Gender)
            .IsInEnum();

        RuleFor(x => x.CountryId)
            .GreaterThan(0);
    }
}
