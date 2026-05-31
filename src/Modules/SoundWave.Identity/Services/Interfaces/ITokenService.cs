using SoundWave.Identity.Dtos;
using System.Security.Claims;

namespace SoundWave.Identity.Services;

/// <summary>
/// Provides pure token operations and an A-to-Z token generation flow that saves to the database.
/// </summary>
internal interface ITokenService
{
    /// <summary>
    /// Generates the JWT access token and a refresh token, saves the refresh token to the database,
    /// and returns both combined.
    /// </summary>
    /// <param name="user">The user profile info.</param>
    /// <param name="previousTokenId">Optional previous refresh token ID to replace (for revocation chaining).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<UserTokensDto> GenerateUserTokensAsync(UserLoginInfoDto user, Guid? previousTokenId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a raw refresh token against its stored BCrypt hash.
    /// </summary>
    /// <param name="rawToken">The raw refresh token to verify.</param>
    /// <param name="storedHash">The stored BCrypt hash to verify against.</param>
    /// <returns>True if the token is valid; otherwise, false.</returns>
    bool VerifyToken(string rawToken, string storedHash);

    /// <summary>
    /// Reads claims from an expired JWT without throwing.
    /// Used in the refresh token flow to identify the user from an expired access token.
    /// Returns null if the token is structurally invalid (not just expired).
    /// </summary>
    /// <param name="accessToken">The expired JWT access token.</param>
    /// <returns>A ClaimsPrincipal containing user claims if valid; otherwise, null.</returns>
    ClaimsPrincipal? ReadExpiredToken(string accessToken);

    /// <summary>
    /// Revokes an active refresh token for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A boolean indicating success.</returns>
    Task<bool> RevokeActiveRefreshToken(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Blacklists a JWT identifier to prevent token reuse after logout.
    /// </summary>
    /// <param name="jti">The JWT ID (jti) claim to blacklist.</param>
    /// <param name="expiryDate">The expiration date/time of the JWT.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BlacklistJtiAsync(string jti, DateTime expiryDate, CancellationToken cancellationToken = default);
}
