using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.Tracks.MoveTrackToAlbum;

/// <summary>
/// Handles moving a track to a target album owned by the same artist.
/// Re-sequences track numbers in both source and target albums and updates track counts.
/// </summary>
internal class MoveTrackToAlbumCommandHandler(
    ICatalogRepository<Track> trackRepository,
    ICatalogRepository<Album> albumRepository,
    ICurrentUserService currentUserService,
    ILogger<MoveTrackToAlbumCommandHandler> logger)
    : IRequestHandler<MoveTrackToAlbumCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        MoveTrackToAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, Guid>.Failure(validation.Error, validation.ErrorMessage);

        var (sourceAlbumId, targetTrackCount) = validation.Data;

        if (sourceAlbumId == request.TargetAlbumId)
        {
            // Already in target album
            return Result<CatalogError, Guid>.Success(request.TrackId);
        }

        await MoveTrackAndReorderAsync(request.TrackId, sourceAlbumId, request.TargetAlbumId, targetTrackCount, cancellationToken);
        return Result<CatalogError, Guid>.Success(request.TrackId);
    }

    #region Private Methods

    private async Task<Result<CatalogError, (Guid SourceAlbumId, int TargetTrackCount)>> ValidateAsync(
        MoveTrackToAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var trackValidation = await trackRepository.GetAll()
            .Where(t => t.Id == request.TrackId)
            .Select(t => new
            {
                Exists = true,
                SourceAlbumId = t.AlbumId,
                IsPrimaryArtist = t.Album.AlbumArtists.Any(aa => aa.Order == 0 && aa.Artist.UserId == userId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (trackValidation is null)
        {
            logger.LogWarning("Move track rejected — track {TrackId} not found", request.TrackId);
            return Result<CatalogError, (Guid, int)>.Failure(CatalogError.TrackNotFound, $"Track '{request.TrackId}' not found.");
        }

        if (!trackValidation.IsPrimaryArtist)
        {
            logger.LogWarning("Move track rejected — user {UserId} is not the primary artist of track {TrackId}", userId, request.TrackId);
            return Result<CatalogError, (Guid, int)>.Failure(CatalogError.UnauthorizedTrackAccess, "Only the primary artist can move this track.");
        }

        var targetAlbumValidation = await albumRepository.GetAll()
            .Where(a => a.Id == request.TargetAlbumId)
            .Select(a => new
            {
                Exists = true,
                IsPrimaryArtist = a.AlbumArtists.Any(aa => aa.Order == 0 && aa.Artist.UserId == userId),
                CurrentTrackCount = a.TrackCount
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (targetAlbumValidation is null)
        {
            logger.LogWarning("Move track rejected — target album {AlbumId} not found", request.TargetAlbumId);
            return Result<CatalogError, (Guid, int)>.Failure(CatalogError.AlbumNotFound, $"Target album '{request.TargetAlbumId}' not found.");
        }

        if (!targetAlbumValidation.IsPrimaryArtist)
        {
            logger.LogWarning("Move track rejected — user {UserId} is not the primary artist of target album {AlbumId}", userId, request.TargetAlbumId);
            return Result<CatalogError, (Guid, int)>.Failure(CatalogError.UnauthorizedAlbumAccess, "Only the primary artist of the target album can move tracks into it.");
        }

        return Result<CatalogError, (Guid, int)>.Success((trackValidation.SourceAlbumId, targetAlbumValidation.CurrentTrackCount));
    }

    private async Task MoveTrackAndReorderAsync(
        Guid trackId,
        Guid sourceAlbumId,
        Guid targetAlbumId,
        int targetTrackCount,
        CancellationToken cancellationToken)
    {
        var track = await trackRepository.GetAll()
            .FirstAsync(t => t.Id == trackId, cancellationToken);

        var newTrackNumber = targetTrackCount + 1;
        track.AlbumId = targetAlbumId;
        track.TrackNumber = newTrackNumber;

        // Re-gap source album tracks
        var remainingSourceTracks = await trackRepository.GetAll()
            .Where(t => t.AlbumId == sourceAlbumId && t.Id != trackId)
            .OrderBy(t => t.TrackNumber)
            .ToListAsync(cancellationToken);

        var seq = 1;
        foreach (var remainingTrack in remainingSourceTracks)
        {
            remainingTrack.TrackNumber = seq++;
        }

        // Update counts on both albums
        var sourceAlbum = new Album { Id = sourceAlbumId, TrackCount = remainingSourceTracks.Count };
        var targetAlbum = new Album { Id = targetAlbumId, TrackCount = newTrackNumber };

        albumRepository.SaveInclude(sourceAlbum, nameof(Album.TrackCount));
        albumRepository.SaveInclude(targetAlbum, nameof(Album.TrackCount));

        await trackRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Track '{TrackId}' moved from Album {SourceAlbumId} to Album {TargetAlbumId} as track #{TrackNumber}",
            trackId, sourceAlbumId, targetAlbumId, newTrackNumber);
    }

    #endregion
}
