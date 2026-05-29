namespace SoundWave.Identity.Dtos;

internal class UserVerificationInfoDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
