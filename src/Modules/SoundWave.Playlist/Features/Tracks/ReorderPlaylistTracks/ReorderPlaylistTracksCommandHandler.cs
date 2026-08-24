using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Tracks.ReorderPlaylistTracks;

/// <summary>
/// Handles moving a track to a new position within a playlist and shifting intermediate tracks.
/// </summary>
internal class ReorderPlaylistTracksCommandHandler(
    IPlaylistRepository<Data.Entities.Playlist> playlistRepository,
    IPlaylistRepository<PlaylistTrack> playlistTrackRepository,
    ICurrentUserService currentUserService,
    ILogger<ReorderPlaylistTracksCommandHandler> logger)
    : IRequestHandler<ReorderPlaylistTracksCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        ReorderPlaylistTracksCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var validation = await ValidateAsync(request, userId, cancellationToken);
        if (!validation.IsSuccess)
            return Result<PlaylistError, bool>.Failure(validation.Error, validation.ErrorMessage);

        var (playlist, targetTrack) = validation.Data!;
        await ShiftAndReorderAsync(playlist.Id, targetTrack, request.NewPosition, userId, cancellationToken);

        return Result<PlaylistError, bool>.Success(true);
    }

    #region Private Methods

    private async Task<Result<PlaylistError, (Data.Entities.Playlist Playlist, PlaylistTrack Track)>> ValidateAsync(
        ReorderPlaylistTracksCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var playlist = await playlistRepository.GetAll()
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, cancellationToken);

        if (playlist is null)
        {
            logger.LogWarning("Reorder track rejected — playlist {PlaylistId} not found", request.PlaylistId);
            return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Failure(
                PlaylistError.PlaylistNotFound, "Playlist not found.");
        }

        if (playlist.IsSystem)
        {
            logger.LogWarning("Reorder track rejected — system playlist {PlaylistId} cannot be reordered directly", request.PlaylistId);
            return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Failure(
                PlaylistError.SystemPlaylistProtected, "System playlists like 'Liked Songs' cannot be reordered directly.");
        }

        if (playlist.OwnerId != userId)
        {
            logger.LogWarning("Reorder track rejected — user {UserId} is not the owner of playlist {PlaylistId}", userId, request.PlaylistId);
            return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Failure(
                PlaylistError.Unauthorized, "You do not have permission to reorder tracks in this playlist.");
        }

        var targetTrack = await playlistTrackRepository.GetAll()
            .Where(pt => pt.PlaylistId == request.PlaylistId && pt.TrackId == request.TrackId)
            .OrderBy(pt => pt.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetTrack is null)
        {
            logger.LogWarning("Reorder track rejected — track {TrackId} not found in playlist {PlaylistId}", request.TrackId, request.PlaylistId);
            return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Failure(
                PlaylistError.TrackNotInPlaylist, "Track is not in this playlist.");
        }

        return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Success((playlist, targetTrack));
    }

    private async Task ShiftAndReorderAsync(
        Guid playlistId,
        PlaylistTrack targetTrack,
        int requestedPosition,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var oldPosition = targetTrack.Position;

        var totalTracks = await playlistTrackRepository.GetAll()
            .CountAsync(pt => pt.PlaylistId == playlistId, cancellationToken);

        var targetPosition = Math.Clamp(requestedPosition, 1, totalTracks);

        if (oldPosition == targetPosition)
            return;

        if (oldPosition < targetPosition)
        {
            // Moving down: Tracks in range (oldPosition, targetPosition] shift UP by 1
            var intermediateTracks = await playlistTrackRepository.GetAll()
                .Where(pt => pt.PlaylistId == playlistId && pt.Position > oldPosition && pt.Position <= targetPosition)
                .ToListAsync(cancellationToken);

            foreach (var track in intermediateTracks)
            {
                track.Position -= 1;
                playlistTrackRepository.SaveInclude(track, nameof(PlaylistTrack.Position));
            }
        }
        else
        {
            // Moving up: Tracks in range [targetPosition, oldPosition) shift DOWN by 1
            var intermediateTracks = await playlistTrackRepository.GetAll()
                .Where(pt => pt.PlaylistId == playlistId && pt.Position >= targetPosition && pt.Position < oldPosition)
                .ToListAsync(cancellationToken);

            foreach (var track in intermediateTracks)
            {
                track.Position += 1;
                playlistTrackRepository.SaveInclude(track, nameof(PlaylistTrack.Position));
            }
        }

        targetTrack.Position = targetPosition;
        playlistTrackRepository.SaveInclude(targetTrack, nameof(PlaylistTrack.Position));

        await playlistTrackRepository.SaveChanges(cancellationToken);

        logger.LogInformation(
            "Track {TrackId} in playlist {PlaylistId} reordered from position {OldPosition} to {NewPosition} by user {UserId}",
            targetTrack.TrackId, playlistId, oldPosition, targetPosition, userId);
    }

    #endregion
}
