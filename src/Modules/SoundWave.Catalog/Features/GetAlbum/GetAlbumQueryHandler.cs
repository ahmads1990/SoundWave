using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.GetAlbum;

/// <summary>
/// Handles retrieving a single album with its tracklist, artists, and genres.
/// </summary>
internal class GetAlbumQueryHandler(
    ICatalogReadRepository<Album> albumReadRepository,
    ILogger<GetAlbumQueryHandler> logger)
    : IRequestHandler<GetAlbumQuery, Result<CatalogError, AlbumDetailsDto>>
{
    public async Task<Result<CatalogError, AlbumDetailsDto>> Handle(
        GetAlbumQuery request,
        CancellationToken cancellationToken)
    {
        var album = await albumReadRepository.GetAll()
            .Where(a => a.Id == request.AlbumId)
            .Select(a => new AlbumDetailsDto(
                a.Id,
                a.Title,
                a.AlbumType,
                a.IsPublished,
                a.ReleaseDate,
                a.CoverImageUrl,
                a.Description,
                a.TrackCount,
                a.Tracks
                    .OrderBy(t => t.TrackNumber)
                    .Select(t => new AlbumTrackDto(
                        t.Id,
                        t.Title,
                        t.DurationSeconds,
                        t.TrackNumber,
                        t.PlayCount,
                        t.LikeCount,
                        t.TrackFile != null ? t.TrackFile.Status : TrackFileStatus.Pending,
                        t.TrackArtists
                            .OrderBy(ta => ta.Order)
                            .Select(ta => new AlbumTrackArtistDto(ta.ArtistId, ta.Artist.StageName, ta.Order))
                            .ToList()))
                    .ToList(),
                a.AlbumGenres
                    .Select(ag => new AlbumGenreDto(ag.GenreId, ag.Genre.Name))
                    .ToList(),
                a.AlbumArtists
                    .OrderBy(aa => aa.Order)
                    .Select(aa => new AlbumArtistDto(aa.ArtistId, aa.Artist.StageName, aa.Order))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (album is null)
        {
            logger.LogWarning("Album {AlbumId} not found", request.AlbumId);
            return Result<CatalogError, AlbumDetailsDto>.Failure(CatalogError.AlbumNotFound, $"Album '{request.AlbumId}' not found.");
        }

        return Result<CatalogError, AlbumDetailsDto>.Success(album);
    }
}
