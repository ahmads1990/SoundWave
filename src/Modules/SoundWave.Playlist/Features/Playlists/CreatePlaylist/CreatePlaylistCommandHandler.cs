using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Playlists.CreatePlaylist;

/// <summary>
/// Handles creating a new playlist for the authenticated user.
/// </summary>
internal class CreatePlaylistCommandHandler(
    IPlaylistRepository<Data.Entities.Playlist> playlistRepository,
    ICurrentUserService currentUserService,
    ILogger<CreatePlaylistCommandHandler> logger)
    : IRequestHandler<CreatePlaylistCommand, Result<PlaylistError, Guid>>
{
    public async Task<Result<PlaylistError, Guid>> Handle(
        CreatePlaylistCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Visibility = request.Visibility,
            IsSystem = false,
            TrackCount = 0,
            TotalDurationSeconds = 0,
            FollowerCount = 0
        };

        await playlistRepository.Add(playlist, cancellationToken);
        await playlistRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Playlist {PlaylistId} ('{Title}') created by user {UserId}", playlist.Id, playlist.Title, userId);

        return Result<PlaylistError, Guid>.Success(playlist.Id);
    }
}
