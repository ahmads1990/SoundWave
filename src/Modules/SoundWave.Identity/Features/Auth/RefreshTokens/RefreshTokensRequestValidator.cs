using FluentValidation;

namespace SoundWave.Identity.Features.Auth.RefreshTokens;

/// <summary>
/// Validator for format and shape of <see cref="RefreshTokensRequest"/>.
/// </summary>
internal class RefreshTokensRequestValidator : AbstractValidator<RefreshTokensRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokensRequestValidator"/> class.
    /// Defines validation rules for RefreshToken.
    /// </summary>
    public RefreshTokensRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
