using System.ComponentModel.DataAnnotations;

namespace SoundWave.Identity.Features.PasswordReset;

/// <summary>
/// Represents the incoming API request for resetting a password.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
/// <param name="Token">The OTP token received via email.</param>
/// <param name="NewPassword">The new password to set.</param>
internal record ResetPasswordRequest(
    [EmailAddress] string Email,
    string Token,
    string NewPassword
);
