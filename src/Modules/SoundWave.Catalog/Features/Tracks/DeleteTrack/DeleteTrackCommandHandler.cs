using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.Tracks.DeleteTrack;

/// <summary>
/// Handles soft-deleting a track from its parent album, updating album track count, and re-sequencing remaining track numbers.
/// Only the primary artist (Order = 0) of the parent album may delete tracks.
/// </summary>
internal class DeleteTrackCommandHandler(
    ICatalogRepository<Track> trackRepository,
    ICatalogRepository<Album> albumRepository,
    ICurrentUserService currentUserService,
    ILogger<DeleteTrackCommandHandler> logger)
    : IRequestHandler<DeleteTrackCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        DeleteTrackCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, Guid>.Failure(validation.Error, validation.ErrorMessage);

        var albumId = validation.Data;
        await DeleteAndReorderAsync(request.TrackId, albumId, cancellationToken);
        return Result<CatalogError, Guid>.Success(request.TrackId);
    }

    #region Private Methods

    private async Task<Result<CatalogError, Guid>> ValidateAsync(
        DeleteTrackCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var trackValidation = await trackRepository.GetAll()
            .Where(t => t.Id == request.TrackId)
            .Select(t => new
            {
                Exists = true,
                AlbumId = t.AlbumId,
                IsPrimaryArtist = t.Album.AlbumArtists.Any(aa => aa.Order == 0 && aa.Artist.UserId == userId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (trackValidation is null)
        {
            logger.LogWarning("Delete track rejected — track {TrackId} not found", request.TrackId);
            return Result<CatalogError, Guid>.Failure(CatalogError.TrackNotFound, $"Track '{request.TrackId}' not found.");
        }

        if (!trackValidation.IsPrimaryArtist)
        {
            logger.LogWarning("Delete track rejected — user {UserId} is not the primary artist of track {TrackId}", userId, request.TrackId);
            return Result<CatalogError, Guid>.Failure(CatalogError.UnauthorizedTrackAccess, "Only the primary artist can delete this track.");
        }

        return Result<CatalogError, Guid>.Success(trackValidation.AlbumId);
    }

    private async Task DeleteAndReorderAsync(
        Guid trackId,
        Guid albumId,
        CancellationToken cancellationToken)
    {
        var track = await trackRepository.GetByID(trackId, cancellationToken);
        if (track is not null)
        {
            trackRepository.SoftDelete(track);
        }

        // Fetch remaining active tracks in the album ordered by original track number
        var remainingTracks = await trackRepository.GetAll()
            .Where(t => t.AlbumId == albumId && t.Id != trackId)
            .OrderBy(t => t.TrackNumber)
            .ToListAsync(cancellationToken);

        // Re-gap track numbers starting from 1
        var seq = 1;
        foreach (var remainingTrack in remainingTracks)
        {
            remainingTrack.TrackNumber = seq++;
        }

        // Update Album track count
        var album = new Album
        {
            Id = albumId,
            TrackCount = remainingTracks.Count
        };
        albumRepository.SaveInclude(album, nameof(Album.TrackCount));

        await trackRepository.SaveChanges(cancellationToken);
        logger.LogInformation("Track '{TrackId}' soft-deleted from Album {AlbumId}. Album track count updated to {TrackCount}", trackId, albumId, remainingTracks.Count);
    }

    #endregion
}
