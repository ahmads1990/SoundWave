using SoundWave.Identity.Common;

namespace SoundWave.Identity.Dtos;

internal class UserBasicInfoDto
{
    public Guid ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsEmailConfirmed { get; set; }
    public UserRole Role { get; set; }
}