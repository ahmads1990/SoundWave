namespace SoundWave.Catalog.Data.Entities.Lookups;

/// <summary>
/// Read-only cross-module lookup entity for a user in the Auth schema.
/// Used by Catalog consumers to resolve user profile details for emails and notifications.
/// </summary>
internal class UserLookup
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public UserProfileLookup? Profile { get; set; }

    public string FullName =>
        Profile is not null && !string.IsNullOrWhiteSpace(Profile.FirstName)
            ? $"{Profile.FirstName} {Profile.LastName}".Trim()
            : Email;
}
