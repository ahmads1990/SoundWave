using SoundWave.Identity.Common;

namespace SoundWave.Identity.Dtos;

/// <summary>
/// Represents the base claims data for a user.
/// </summary>
/// <param name="Uid">The unique identifier of the user.</param>
/// <param name="Role">The role of the user.</param>
/// <param name="Name">The name of the user.</param>
/// <param name="Email">The email address of the user.</param>
internal record UserTokenBaseClaims(Guid Uid, UserRole Role, string Name, string Email)
{
    /// <summary>
    /// Validates that all claims are not null or empty.
    /// </summary>
    /// <returns>True if any claim is invalid, otherwise false.</returns>
    public bool AreClaimsInValid()
    {
        return Uid == Guid.Empty ||
               string.IsNullOrEmpty(Name) ||
               string.IsNullOrEmpty(Email);
    }
}