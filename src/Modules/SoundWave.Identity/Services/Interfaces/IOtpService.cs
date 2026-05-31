namespace SoundWave.Identity.Services;

/// <summary>
/// Generates and verifies OTP codes for email verification and password reset.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a numeric OTP of the given length (default 6 digits).
    /// </summary>
    string GenerateOtp(int length = 6);
}
