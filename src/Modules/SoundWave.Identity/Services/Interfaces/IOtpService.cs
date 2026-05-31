namespace SoundWave.Identity.Services;

/// <summary>
/// Generates and verifies OTP codes for email verification and password reset.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a numeric OTP of the given length (default 6 digits).
    /// </summary>
    /// <param name="length">The length of the generated OTP code (default 6).</param>
    /// <returns>A string containing the generated OTP code.</returns>
    string GenerateOtp(int length = 6);
}
