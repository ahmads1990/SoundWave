using System.ComponentModel.DataAnnotations;

namespace SoundWave.Identity.Features.Account.PasswordReset;

/// <summary>
/// Represents the incoming API request for initiating a password reset.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
internal record ForgotPasswordRequest(
    [EmailAddress] string Email
);
