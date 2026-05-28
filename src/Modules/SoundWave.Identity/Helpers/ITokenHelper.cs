using SoundWave.Identity.Dtos;

namespace SoundWave.Identity.Helpers;

/// <summary>
/// Provides methods for generating tokens and OTPs.
/// </summary>
internal interface ITokenHelper
{
    /// <summary>
    /// Generates a JSON Web Token (JWT) for a user.
    /// </summary>
    /// <param name="baseClaims">Basic user claims.</param>
    /// <param name="userClaims">Additional custom user claims.</param>
    /// <param name="expiresInMinutes">Token expiration time in minutes.</param>
    /// <returns>A signed JWT string.</returns>
    string GenerateJWT(UserTokenBaseClaims baseClaims, List<UserClaim> userClaims, int expiresInMinutes = 0);

    /// <summary>
    /// Generates a numeric one-time password (OTP).
    /// </summary>
    /// <param name="length">The length of the OTP.</param>
    /// <returns>A numeric OTP string.</returns>
    string GenerateOTP(int length = 6);

    /// <summary>
    /// Generates and persists a cryptographically secure refresh token for the user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="tokenId">Optional identifier for updating an existing refresh token instead of inserting a new one.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The plain-text refresh token to be returned to the client.</returns>
    Task<string> GenerateAndSaveRefreshTokenAsync(Guid userId, Guid? tokenId = null, CancellationToken cancellationToken = default);
}