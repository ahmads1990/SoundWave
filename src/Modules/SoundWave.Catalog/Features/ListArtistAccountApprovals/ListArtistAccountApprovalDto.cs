using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.ListArtistAccountApprovals;

internal record ListArtistAccountApprovalDto(
    Guid Id,
    Guid UserId,
    string StageName,
    string? Bio,
    ArtistApprovalStatus Status,
    string? RejectionReason,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    DateTime CreatedDate);
