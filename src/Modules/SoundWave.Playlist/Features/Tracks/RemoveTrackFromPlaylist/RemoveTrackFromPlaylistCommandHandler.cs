using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Tracks.RemoveTrackFromPlaylist;

/// <summary>
/// Handles removing a track from a playlist, soft-deleting the junction row,
/// re-gapping subsequent positions, and updating the denormalized track count.
/// </summary>
internal class RemoveTrackFromPlaylistCommandHandler(
    IPlaylistRepository<Data.Entities.Playlist> playlistRepository,
    IPlaylistRepository<PlaylistTrack> playlistTrackRepository,
    ICurrentUserService currentUserService,
    ILogger<RemoveTrackFromPlaylistCommandHandler> logger)
    : IRequestHandler<RemoveTrackFromPlaylistCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        RemoveTrackFromPlaylistCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var validation = await ValidateAsync(request, userId, cancellationToken);
        if (!validation.IsSuccess)
            return Result<PlaylistError, bool>.Failure(validation.Error, validation.ErrorMessage);

        var (playlist, playlistTrack) = validation.Data!;
        await RemoveAndRegapAsync(playlist, playlistTrack, userId, cancellationToken);

        return Result<PlaylistError, bool>.Success(true);
    }

    #region Private Methods

    private async Task<Result<PlaylistError, (Data.Entities.Playlist Playlist, PlaylistTrack Track)>> ValidateAsync(
        RemoveTrackFromPlaylistCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var playlist = await playlistRepository.GetAll()
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, cancellationToken);

        if (playlist is null)
        {
            logger.LogWarning("Remove track rejected — playlist {PlaylistId} not found", request.PlaylistId);
            return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Failure(
                PlaylistError.PlaylistNotFound, "Playlist not found.");
        }

        if (playlist.IsSystem)
        {
            logger.LogWarning("Remove track rejected — system playlist {PlaylistId} cannot be modified directly", request.PlaylistId);
            return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Failure(
                PlaylistError.SystemPlaylistProtected, "System playlists like 'Liked Songs' cannot be modified directly.");
        }

        if (playlist.OwnerId != userId)
        {
            logger.LogWarning("Remove track rejected — user {UserId} is not the owner of playlist {PlaylistId}", userId, request.PlaylistId);
            return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Failure(
                PlaylistError.Unauthorized, "You do not have permission to remove tracks from this playlist.");
        }

        var playlistTrack = await playlistTrackRepository.GetAll()
            .Where(pt => pt.PlaylistId == request.PlaylistId && pt.TrackId == request.TrackId)
            .OrderBy(pt => pt.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (playlistTrack is null)
        {
            logger.LogWarning("Remove track rejected — track {TrackId} not found in playlist {PlaylistId}", request.TrackId, request.PlaylistId);
            return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Failure(
                PlaylistError.TrackNotInPlaylist, "Track is not in this playlist.");
        }

        return Result<PlaylistError, (Data.Entities.Playlist, PlaylistTrack)>.Success((playlist, playlistTrack));
    }

    private async Task RemoveAndRegapAsync(
        Data.Entities.Playlist playlist,
        PlaylistTrack playlistTrack,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var removedPosition = playlistTrack.Position;

        playlistTrackRepository.SoftDelete(playlistTrack);

        var subsequentTracks = await playlistTrackRepository.GetAll()
            .Where(pt => pt.PlaylistId == playlist.Id && pt.Position > removedPosition)
            .ToListAsync(cancellationToken);

        foreach (var track in subsequentTracks)
        {
            track.Position -= 1;
            playlistTrackRepository.SaveInclude(track, nameof(PlaylistTrack.Position));
        }

        playlist.TrackCount = Math.Max(0, playlist.TrackCount - 1);
        playlistRepository.SaveInclude(playlist, nameof(Data.Entities.Playlist.TrackCount));

        await playlistRepository.SaveChanges(cancellationToken);

        logger.LogInformation(
            "Track {TrackId} removed from playlist {PlaylistId} (was position {Position}) by user {UserId}",
            playlistTrack.TrackId, playlist.Id, removedPosition, userId);
    }

    #endregion
}
