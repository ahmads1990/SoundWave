namespace SoundWave.Identity.Features.Account.ResendVerificationEmail;

/// <summary>
/// Request model for resending the email verification OTP.
/// </summary>
/// <param name="Email">The email address of the user requesting a new OTP.</param>
public record ResendVerificationEmailRequest(string Email);
