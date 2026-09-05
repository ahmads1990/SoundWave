namespace SoundWave.Identity.Features.Account.UpdateProfileImages;

/// <summary>
/// Request contract for updating a user's profile and cover image URLs.
/// </summary>
public record UpdateProfileImagesRequest(string? ProfilePicUrl, string? CoverImageUrl);
