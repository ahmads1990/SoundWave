using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Albums.GetNewReleases;

/// <summary>
/// Query for retrieving a paginated list of the most recently released published albums with optional filters.
/// </summary>
internal record GetNewReleasesQuery : BasePaginatedQuery, IRequest<Result<CatalogError, PaginatedResponse<AlbumSummaryDto>>>
{
    public int? GenreId { get; init; }
    public AlbumType? AlbumType { get; init; }
    public int? DaysOld { get; init; }
}
