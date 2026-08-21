namespace SoundWave.Identity.Features.Auth.Login;

/// <summary>
/// Represents the API request payload for user authentication.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
/// <param name="Password">The user's plain-text password.</param>
internal record LoginRequest(
    string Email,
    string Password
    );