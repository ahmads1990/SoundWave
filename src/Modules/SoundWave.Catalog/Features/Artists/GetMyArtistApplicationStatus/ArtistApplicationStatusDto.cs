using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.Artists.GetMyArtistApplicationStatus;

internal record ArtistApplicationStatusDto(
    Guid Id,
    Guid UserId,
    string StageName,
    string? Bio,
    ArtistApprovalStatus Status,
    string? RejectionReason,
    DateTime? ReviewedAt,
    DateTime CreatedDate);
