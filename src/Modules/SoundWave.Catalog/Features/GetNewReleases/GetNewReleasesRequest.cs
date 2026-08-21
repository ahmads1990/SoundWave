using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Models.Requests;

namespace SoundWave.Catalog.Features.GetNewReleases;

/// <summary>
/// Query string parameters for retrieving paginated new album releases.
/// </summary>
internal record GetNewReleasesRequest : BasePaginatedRequest
{
    public int? GenreId { get; init; }
    public AlbumType? AlbumType { get; init; }
    public int? DaysOld { get; init; }
}
