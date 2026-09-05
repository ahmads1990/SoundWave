namespace SoundWave.Catalog.Data.Entities.Lookups;

/// <summary>
/// Read-only cross-module lookup entity for a user profile in the Auth schema.
/// Used by Catalog consumers to resolve user first and last name for emails.
/// </summary>
internal class UserProfileLookup
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
