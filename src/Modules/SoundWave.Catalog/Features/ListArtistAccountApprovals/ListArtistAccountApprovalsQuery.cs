using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.ListArtistAccountApprovals;

internal record ListArtistAccountApprovalsQuery : BasePaginatedQuery, IRequest<Result<CatalogError, PaginatedResponse<ListArtistAccountApprovalDto>>>
{
    public string? StageName { get; init; }
    public ArtistApprovalStatus? Status { get; init; } = ArtistApprovalStatus.Pending;
}
