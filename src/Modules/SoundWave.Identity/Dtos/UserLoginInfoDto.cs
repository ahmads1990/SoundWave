using SoundWave.Identity.Common;

using SoundWave.SharedKernel.Common;
namespace SoundWave.Identity.Dtos;

internal class UserLoginInfoDto
{
    public Guid Id { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsLocked { get; set; }
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
