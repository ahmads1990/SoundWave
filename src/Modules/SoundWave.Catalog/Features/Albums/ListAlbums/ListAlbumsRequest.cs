using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Models.Requests;

namespace SoundWave.Catalog.Features.Albums.ListAlbums;

/// <summary>
/// Query string request for listing albums with filtering, sorting, and pagination.
/// </summary>
internal record ListAlbumsRequest : BasePaginatedRequest
{
    public string? Title { get; init; }
    public int? GenreId { get; init; }
    public Guid? ArtistId { get; init; }
    public bool? IsPublished { get; init; }
    public AlbumType? AlbumType { get; init; }

    public static readonly IReadOnlyList<string> AllowedSortFields =
    [
        nameof(Album.Title),
        nameof(Album.ReleaseDate),
        nameof(Album.TrackCount)
    ];
}
