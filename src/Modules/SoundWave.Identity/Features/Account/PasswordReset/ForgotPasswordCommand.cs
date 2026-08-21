using MediatR;
using SoundWave.Identity.Common;

using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.Account.PasswordReset;

/// <summary>
/// Represents the internal MediatR command for initiating a password reset.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
internal record ForgotPasswordCommand(
    string Email
) : IRequest<Result<IdentityError, bool>>;
