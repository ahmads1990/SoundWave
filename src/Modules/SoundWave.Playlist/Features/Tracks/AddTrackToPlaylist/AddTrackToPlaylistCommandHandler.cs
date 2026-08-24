using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Tracks.AddTrackToPlaylist;

/// <summary>
/// Handles appending a track to the end of a playlist.
/// </summary>
internal class AddTrackToPlaylistCommandHandler(
    IPlaylistRepository<Data.Entities.Playlist> playlistRepository,
    IPlaylistRepository<PlaylistTrack> playlistTrackRepository,
    ICurrentUserService currentUserService,
    ILogger<AddTrackToPlaylistCommandHandler> logger)
    : IRequestHandler<AddTrackToPlaylistCommand, Result<PlaylistError, Guid>>
{
    public async Task<Result<PlaylistError, Guid>> Handle(
        AddTrackToPlaylistCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var validation = await ValidateAsync(request, userId, cancellationToken);
        if (!validation.IsSuccess)
            return Result<PlaylistError, Guid>.Failure(validation.Error, validation.ErrorMessage);

        var trackId = await AppendTrackAsync(validation.Data!, request.TrackId, userId, cancellationToken);
        return Result<PlaylistError, Guid>.Success(trackId);
    }

    #region Private Methods

    private async Task<Result<PlaylistError, Data.Entities.Playlist>> ValidateAsync(
        AddTrackToPlaylistCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var playlist = await playlistRepository.GetAll()
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, cancellationToken);

        if (playlist is null)
        {
            logger.LogWarning("Add track rejected — playlist {PlaylistId} not found", request.PlaylistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.PlaylistNotFound, "Playlist not found.");
        }

        if (playlist.IsSystem)
        {
            logger.LogWarning("Add track rejected — system playlist {PlaylistId} cannot be modified directly", request.PlaylistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.SystemPlaylistProtected, "System playlists like 'Liked Songs' cannot be modified directly.");
        }

        if (playlist.OwnerId != userId)
        {
            logger.LogWarning("Add track rejected — user {UserId} is not the owner of playlist {PlaylistId}", userId, request.PlaylistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.Unauthorized, "You do not have permission to add tracks to this playlist.");
        }

        var alreadyExists = await playlistTrackRepository.GetAll()
            .AnyAsync(pt => pt.PlaylistId == request.PlaylistId && pt.TrackId == request.TrackId, cancellationToken);

        if (alreadyExists)
        {
            logger.LogWarning("Add track rejected — track {TrackId} already in playlist {PlaylistId}", request.TrackId, request.PlaylistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.TrackAlreadyInPlaylist, "Track is already in this playlist.");
        }

        return Result<PlaylistError, Data.Entities.Playlist>.Success(playlist);
    }

    private async Task<Guid> AppendTrackAsync(
        Data.Entities.Playlist playlist,
        Guid trackId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var nextPosition = playlist.TrackCount + 1;

        var playlistTrack = new PlaylistTrack
        {
            PlaylistId = playlist.Id,
            TrackId = trackId,
            Position = nextPosition,
            AddedBy = userId,
            AddedAt = DateTime.UtcNow
        };

        await playlistTrackRepository.Add(playlistTrack, cancellationToken);

        playlist.TrackCount += 1;
        playlistRepository.SaveInclude(playlist, nameof(Data.Entities.Playlist.TrackCount));

        await playlistRepository.SaveChanges(cancellationToken);

        logger.LogInformation(
            "Track {TrackId} added to playlist {PlaylistId} at position {Position} by user {UserId}",
            trackId, playlist.Id, nextPosition, userId);

        return playlistTrack.Id;
    }

    #endregion
}
