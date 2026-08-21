using MediatR;
using SoundWave.Identity.Common;

using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.Account.PasswordReset;

/// <summary>
/// Represents the internal MediatR command for resetting a password.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
/// <param name="Token">The OTP token received via email.</param>
/// <param name="NewPassword">The new password to set.</param>
internal record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : IRequest<Result<IdentityError, bool>>;
