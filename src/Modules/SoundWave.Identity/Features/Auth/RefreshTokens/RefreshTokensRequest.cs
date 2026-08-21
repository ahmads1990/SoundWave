namespace SoundWave.Identity.Features.Auth.RefreshTokens;

/// <summary>
/// Represents the API request payload for token refresh.
/// </summary>
/// <param name="UserId">The ID of the user requesting the refresh.</param>
/// <param name="RefreshToken">The refresh token provided by the client.</param>
internal record RefreshTokensRequest(
    string UserId,
    string RefreshToken
    );
