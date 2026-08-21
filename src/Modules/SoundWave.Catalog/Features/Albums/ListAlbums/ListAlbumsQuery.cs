using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Albums.ListAlbums;

/// <summary>
/// Query for a paginated, filterable list of albums.
/// </summary>
internal record ListAlbumsQuery : BasePaginatedQuery, IRequest<Result<CatalogError, PaginatedResponse<AlbumSummaryListDto>>>
{
    public string? Title { get; init; }
    public int? GenreId { get; init; }
    public Guid? ArtistId { get; init; }
    public bool? IsPublished { get; init; }
    public AlbumType? AlbumType { get; init; }
}
