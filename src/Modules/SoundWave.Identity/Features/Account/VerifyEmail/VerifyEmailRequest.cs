namespace SoundWave.Identity.Features.Account.VerifyEmail;

/// <summary>
/// Request model for verifying a user's email address.
/// </summary>
/// <param name="Email">The email address of the user.</param>
/// <param name="Otp">The 6-digit OTP received via email.</param>
public record VerifyEmailRequest(string Email, string Otp);
