using System.Security.Cryptography;
using System.Text;

namespace SoundWave.Identity.Services;

internal sealed class OtpService : IOtpService
{
    /// <inheritdoc />
    public string GenerateOtp(int length = 6)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than zero.", nameof(length));

        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(RandomNumberGenerator.GetInt32(0, 10));

        return sb.ToString();
    }
}
