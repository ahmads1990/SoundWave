using FluentValidation;

namespace SoundWave.Identity.Features.Login;

/// <summary>
/// Validator for business/database rules of <see cref="LoginCommand"/>.
/// </summary>
internal class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandValidator"/> class.
    /// </summary>
    public LoginCommandValidator()
    {

    }
}
