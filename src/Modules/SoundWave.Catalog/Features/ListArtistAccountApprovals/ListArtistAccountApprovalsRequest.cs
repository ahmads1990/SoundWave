using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Models.Requests;

namespace SoundWave.Catalog.Features.ListArtistAccountApprovals;

internal record ListArtistAccountApprovalsRequest : BasePaginatedRequest
{
    public string? StageName { get; init; }
    public ArtistApprovalStatus? Status { get; init; } = ArtistApprovalStatus.Pending;

    public static readonly IReadOnlyList<string> AllowedSortFields = [
        nameof(ArtistAccountApproval.StageName),
        nameof(ArtistAccountApproval.Status),
        nameof(ArtistAccountApproval.CreatedDate),
        nameof(ArtistAccountApproval.ReviewedAt)
    ];
}
