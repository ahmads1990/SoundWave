using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Entities;

namespace SoundWave.Catalog.Data.Entities;

/// <summary>
/// Tracks the full lifecycle of a user's application to become an artist.
/// An <see cref="Artist"/> row is only created when Status transitions to <see cref="ArtistApprovalStatus.Approved"/>.
/// <para>
/// Cross-module refs — no DB-level FK constraints:
/// <list type="bullet">
/// <item><description><c>UserId</c> → Identity.Users (applicant)</description></item>
/// <item><description><c>ReviewedBy</c> → Identity.Users (admin)</description></item>
/// </list>
/// </para>
/// </summary>
internal class ArtistAccountApproval : BaseEntity
{
    /// <summary>Cross-module ref to Identity.Users. UNIQUE — one active application per user.</summary>
    public Guid UserId { get; set; }

    public string StageName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public ArtistApprovalStatus Status { get; set; } = ArtistApprovalStatus.Pending;

    /// <summary>Populated by admin when Status = Rejected.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Cross-module ref to the admin Identity.Users who reviewed this application.</summary>
    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
