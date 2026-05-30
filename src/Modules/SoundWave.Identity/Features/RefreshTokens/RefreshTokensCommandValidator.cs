using FluentValidation;

namespace SoundWave.Identity.Features.RefreshTokens;

/// <summary>
/// Validator for business/database rules of <see cref="RefreshTokensCommand"/>.
/// </summary>
internal class RefreshTokensCommandValidator : AbstractValidator<RefreshTokensCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokensCommandValidator"/> class.
    /// </summary>
    public RefreshTokensCommandValidator()
    {
    }
}
