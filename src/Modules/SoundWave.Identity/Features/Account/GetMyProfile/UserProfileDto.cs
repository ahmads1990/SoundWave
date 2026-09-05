namespace SoundWave.Identity.Features.Account.GetMyProfile;

/// <summary>
/// DTO representing a user's profile information.
/// </summary>
public record UserProfileDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string? ProfilePicUrl,
    string? CoverImageUrl,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string Language,
    string Gender,
    int? CountryId);
