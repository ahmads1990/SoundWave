using SoundWave.Identity.Common;

namespace SoundWave.Identity.Features.Register;

/// <summary>
/// Represents the client-side request payload for user registration.
/// </summary>
/// <param name="Email">The unique email address of the user.</param>
/// <param name="Password">The plain-text password chosen by the user (Min 8, Max 128 chars, requires uppercase, lowercase, digit, and special char).</param>
/// <param name="FirstName">The user's first name (Max 100 chars).</param>
/// <param name="LastName">The user's last name (Max 100 chars).</param>
/// <param name="DisplayName">The user's preferred display name (Max 100 chars).</param>
/// <param name="DateOfBirth">The user's date of birth, if provided (YYYY-MM-DD format).</param>
/// <param name="Gender">The user's gender identification: 0 = Male, 1 = Female, 2 = Other.</param>
/// <param name="CountryId">The identifier for the user's country of residence (must be greater than 0).</param>
internal record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string DisplayName,
    DateOnly? DateOfBirth,
    Gender Gender,
    int CountryId);
