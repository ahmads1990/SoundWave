using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Identity.Data.Entites;

internal class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsLocked { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public UserProfile? UserProfile { get; set; }
    public AdminProfile? AdminProfile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
