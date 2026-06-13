using MediatR;
using SoundWave.Identity.Common;

namespace SoundWave.Identity.Features.PasswordReset;

/// <summary>
/// Represents the internal MediatR command for initiating a password reset.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
internal record ForgotPasswordCommand(
    string Email
) : IRequest<IdentityResult<bool>>;
