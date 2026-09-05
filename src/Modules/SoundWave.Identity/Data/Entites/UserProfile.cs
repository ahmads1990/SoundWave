using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Identity.Data.Entites;

internal class UserProfile : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePicUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Language { get; set; } = "en";
    public Gender Gender { get; set; }
    public Guid UserId { get; set; }
    public int? CountryId { get; set; }
    public User User { get; set; } = default!;
    public Country? Country { get; set; }
}