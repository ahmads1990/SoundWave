using SoundWave.SharedKernel.Entities;

namespace SoundWave.Identity.Data.Entites;

internal class AdminProfile : BaseEntity
{
    public string Department { get; set; } = string.Empty;
    public bool CanApproveArtists { get; set; }
    public bool CanLockUsers { get; set; }
    public bool CanViewAuditLogs { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
}
