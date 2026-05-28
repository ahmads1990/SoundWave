namespace SoundWave.Identity.Features.Login;

/// <summary>
/// Represents the JWT and refresh token pair returned after successful authentication.
/// </summary>
internal class UserTokensDto
{
    /// <summary>
    /// The short-lived JWT access token used for authenticating API requests.
    /// </summary>
    public string JwtToken { get; set; } = string.Empty;

    /// <summary>
    /// The long-lived refresh token used to obtain new JWT tokens without re-authenticating.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// The user ID (populated on specific states like unverified email).
    /// </summary>
    public Guid? UserId { get; set; }
}
