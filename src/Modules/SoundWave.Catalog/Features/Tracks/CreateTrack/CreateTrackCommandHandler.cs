using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.Tracks.CreateTrack;

/// <summary>
/// Handles creating a new track (metadata only) within an album. Audio is uploaded separately via the upload-url flow.
/// Only the album's primary artist (Order = 0) may create tracks.
/// </summary>
internal class CreateTrackCommandHandler(
    ICatalogRepository<Track> trackRepository,
    ICatalogRepository<Album> albumRepository,
    ISender sender,
    ICurrentUserService currentUserService,
    ILogger<CreateTrackCommandHandler> logger)
    : IRequestHandler<CreateTrackCommand, Result<CatalogError, CreateTrackResponse>>
{
    public async Task<Result<CatalogError, CreateTrackResponse>> Handle(
        CreateTrackCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, CreateTrackResponse>.Failure(validation.Error, validation.ErrorMessage);

        var (primaryArtistId, nextTrackNumber) = validation.Data;
        var response = await CreateAndSaveTrackAsync(request, primaryArtistId, nextTrackNumber, cancellationToken);
        return Result<CatalogError, CreateTrackResponse>.Success(response);
    }

    #region Private Methods

    private async Task<Result<CatalogError, (Guid PrimaryArtistId, int NextTrackNumber)>> ValidateAsync(
        CreateTrackCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var albumValidation = await albumRepository.GetAll()
            .Where(a => a.Id == request.AlbumId)
            .Select(a => new
            {
                Exists = true,
                IsPublished = a.IsPublished,
                PrimaryArtistId = a.AlbumArtists
                    .Where(aa => aa.Order == 0 && aa.Artist.UserId == userId)
                    .Select(aa => (Guid?)aa.ArtistId)
                    .FirstOrDefault(),
                NextTrackNumber = a.TrackCount + 1
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (albumValidation is null)
        {
            logger.LogWarning("Create track rejected — album {AlbumId} not found", request.AlbumId);
            return Result<CatalogError, (Guid, int)>.Failure(CatalogError.AlbumNotFound, $"Album '{request.AlbumId}' not found.");
        }

        if (albumValidation.PrimaryArtistId is null)
        {
            logger.LogWarning("Create track rejected — user {UserId} is not the primary artist of album {AlbumId}", userId, request.AlbumId);
            return Result<CatalogError, (Guid, int)>.Failure(CatalogError.UnauthorizedAlbumAccess, "Only the primary artist can add tracks to this album.");
        }

        if (albumValidation.IsPublished)
        {
            logger.LogWarning("Create track rejected — album {AlbumId} is already published", request.AlbumId);
            return Result<CatalogError, (Guid, int)>.Failure(CatalogError.AlbumAlreadyPublished, "Cannot add tracks to an album that has already been published.");
        }

        if (request.GenreIds is { Count: > 0 })
        {
            var allGenresExist = await sender.Send(new ValidateGenresExistQuery(request.GenreIds), cancellationToken);
            if (!allGenresExist)
            {
                logger.LogWarning("Create track rejected — one or more genre IDs are invalid");
                return Result<CatalogError, (Guid, int)>.Failure(CatalogError.InvalidGenreId, "One or more specified genre IDs do not exist.");
            }
        }

        return Result<CatalogError, (Guid, int)>.Success((albumValidation.PrimaryArtistId.Value, albumValidation.NextTrackNumber));
    }

    private async Task<CreateTrackResponse> CreateAndSaveTrackAsync(
        CreateTrackCommand request,
        Guid primaryArtistId,
        int nextTrackNumber,
        CancellationToken cancellationToken)
    {
        var track = new Track
        {
            Id = Guid.CreateVersion7(),
            AlbumId = request.AlbumId,
            Title = request.Title,
            TrackNumber = nextTrackNumber,
            PlayCount = 0,
            LikeCount = 0
        };

        // Primary artist (Order = 0)
        track.TrackArtists.Add(new TrackArtist { ArtistId = primaryArtistId, Order = 0 });

        // Featured artists
        if (request.FeaturedArtistIds is { Count: > 0 })
        {
            var order = 1;
            foreach (var featuredId in request.FeaturedArtistIds.Distinct().Where(id => id != primaryArtistId))
            {
                track.TrackArtists.Add(new TrackArtist { ArtistId = featuredId, Order = order++ });
            }
        }

        // Genres
        if (request.GenreIds is { Count: > 0 })
        {
            foreach (var genreId in request.GenreIds.Distinct())
            {
                track.TrackGenres.Add(new TrackGenre { GenreId = genreId });
            }
        }

        await trackRepository.Add(track, cancellationToken);

        // Update denormalized track count directly on Album via SaveInclude
        var album = new Album
        {
            Id = request.AlbumId,
            TrackCount = nextTrackNumber
        };
        albumRepository.SaveInclude(album, nameof(Album.TrackCount));

        await trackRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Track '{TrackTitle}' ({TrackId}) created in album {AlbumId} as track #{TrackNumber}", track.Title, track.Id, request.AlbumId, track.TrackNumber);

        return new CreateTrackResponse(track.Id, track.TrackNumber);
    }

    #endregion
}
