using SoundWave.Identity.Common;

namespace SoundWave.Identity.Features.Register;

/// <summary>
/// Represents the client-side request payload for user registration.
/// </summary>
/// <param name="Email">The unique email address of the user.</param>
/// <param name="Password">The plain-text password chosen by the user.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="DisplayName">The user's preferred display name.</param>
/// <param name="DateOfBirth">The user's date of birth, if provided.</param>
/// <param name="Gender">The user's gender identification.</param>
/// <param name="CountryId">The identifier for the user's country of residence.</param>
internal record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string DisplayName,
    DateOnly? DateOfBirth,
    Gender Gender,
    int CountryId);
