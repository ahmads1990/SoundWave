using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.Tracks.EditTrackMetadata;

/// <summary>
/// Handles updating a track's title, duration, genres, and featured artists.
/// Only the primary artist of the parent album may edit tracks.
/// </summary>
internal class EditTrackMetadataCommandHandler(
    ICatalogRepository<Track> trackRepository,
    ISender sender,
    ICurrentUserService currentUserService,
    ILogger<EditTrackMetadataCommandHandler> logger)
    : IRequestHandler<EditTrackMetadataCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        EditTrackMetadataCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, Guid>.Failure(validation.Error, validation.ErrorMessage);

        await UpdateTrackAsync(request, cancellationToken);
        return Result<CatalogError, Guid>.Success(request.TrackId);
    }

    #region Private Methods

    private async Task<Result<CatalogError, Guid>> ValidateAsync(
        EditTrackMetadataCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var trackValidation = await trackRepository.GetAll()
            .Where(t => t.Id == request.TrackId)
            .Select(t => new
            {
                Exists = true,
                IsPrimaryArtist = t.Album.AlbumArtists.Any(aa => aa.Order == 0 && aa.Artist.UserId == userId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (trackValidation is null)
        {
            logger.LogWarning("Edit track rejected — track {TrackId} not found", request.TrackId);
            return Result<CatalogError, Guid>.Failure(CatalogError.TrackNotFound, $"Track '{request.TrackId}' not found.");
        }

        if (!trackValidation.IsPrimaryArtist)
        {
            logger.LogWarning("Edit track rejected — user {UserId} is not the primary artist of track {TrackId}", userId, request.TrackId);
            return Result<CatalogError, Guid>.Failure(CatalogError.UnauthorizedTrackAccess, "Only the primary artist can edit this track.");
        }

        if (request.GenreIds is { Count: > 0 })
        {
            var allGenresExist = await sender.Send(new ValidateGenresExistQuery(request.GenreIds), cancellationToken);
            if (!allGenresExist)
            {
                logger.LogWarning("Edit track rejected — one or more genre IDs are invalid");
                return Result<CatalogError, Guid>.Failure(CatalogError.InvalidGenreId, "One or more specified genre IDs do not exist.");
            }
        }

        return Result<CatalogError, Guid>.Success(request.TrackId);
    }

    private async Task UpdateTrackAsync(
        EditTrackMetadataCommand request,
        CancellationToken cancellationToken)
    {
        var track = await trackRepository.GetAll()
            .Include(t => t.TrackArtists)
            .Include(t => t.TrackGenres)
            .FirstAsync(t => t.Id == request.TrackId, cancellationToken);

        track.Title = request.Title;
        track.DurationSeconds = request.DurationSeconds;

        // Sync TrackGenres
        track.TrackGenres.Clear();
        if (request.GenreIds is { Count: > 0 })
        {
            foreach (var genreId in request.GenreIds.Distinct())
            {
                track.TrackGenres.Add(new TrackGenre { GenreId = genreId });
            }
        }

        // Sync TrackArtists — keep primary (Order = 0), replace featured
        var primaryArtist = track.TrackArtists.FirstOrDefault(ta => ta.Order == 0);
        track.TrackArtists.Clear();
        if (primaryArtist is not null)
        {
            track.TrackArtists.Add(primaryArtist);
        }

        if (request.FeaturedArtistIds is { Count: > 0 })
        {
            var order = 1;
            foreach (var featuredId in request.FeaturedArtistIds.Distinct().Where(id => primaryArtist == null || id != primaryArtist.ArtistId))
            {
                track.TrackArtists.Add(new TrackArtist { ArtistId = featuredId, Order = order++ });
            }
        }

        await trackRepository.SaveChanges(cancellationToken);
        logger.LogInformation("Track '{TrackTitle}' ({TrackId}) metadata updated", track.Title, track.Id);
    }

    #endregion
}
