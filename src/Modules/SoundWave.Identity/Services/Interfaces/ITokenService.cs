using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using SoundWave.Identity.Dtos;

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
    bool VerifyToken(string rawToken, string storedHash);

    /// <summary>
    /// Reads claims from an expired JWT without throwing.
    /// Used in the refresh token flow to identify the user from an expired access token.
    /// Returns null if the token is structurally invalid (not just expired).
    /// </summary>
    ClaimsPrincipal? ReadExpiredToken(string accessToken);
}
