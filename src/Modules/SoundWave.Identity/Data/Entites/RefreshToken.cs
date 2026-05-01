using SoundWave.SharedKernel.Entities;

namespace SoundWave.Identity.Data.Entites;

internal class RefreshToken : BaseEntity
{
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
}
