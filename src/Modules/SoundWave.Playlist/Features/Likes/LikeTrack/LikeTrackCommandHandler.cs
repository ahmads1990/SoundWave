using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data;
using SoundWave.Playlist.Data.Entities;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Likes.LikeTrack;

/// <summary>
/// Handles liking a track for the authenticated user and synchronizing with their system "Liked Songs" playlist.
/// </summary>
internal class LikeTrackCommandHandler(
    PlaylistDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<LikeTrackCommandHandler> logger)
    : IRequestHandler<LikeTrackCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        LikeTrackCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        await LikeTrackAsync(userId, request.TrackId, cancellationToken);

        logger.LogInformation("Track {TrackId} liked by user {UserId}", request.TrackId, userId);
        return Result<PlaylistError, bool>.Success(true);
    }

    #region Private Methods

    private async Task LikeTrackAsync(
        Guid userId,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var alreadyLiked = await dbContext.LikedTracks
            .AnyAsync(lt => lt.UserId == userId && lt.TrackId == trackId, cancellationToken);

        if (!alreadyLiked)
        {
            dbContext.LikedTracks.Add(new LikedTrack
            {
                UserId = userId,
                TrackId = trackId,
                LikedAt = DateTime.UtcNow
            });
        }

        var likedSongsPlaylist = await dbContext.Playlists
            .FirstOrDefaultAsync(p => p.OwnerId == userId && p.IsSystem && !p.IsDeleted, cancellationToken);

        if (likedSongsPlaylist is null)
        {
            likedSongsPlaylist = new Data.Entities.Playlist
            {
                OwnerId = userId,
                Title = "Liked Songs",
                Description = "Your favorite tracks",
                Visibility = PlaylistVisibility.Private,
                IsSystem = true,
                TrackCount = 0,
                TotalDurationSeconds = 0,
                FollowerCount = 0
            };
            dbContext.Playlists.Add(likedSongsPlaylist);
        }

        var trackInPlaylist = await dbContext.PlaylistTracks
            .AnyAsync(pt => pt.PlaylistId == likedSongsPlaylist.Id && pt.TrackId == trackId && !pt.IsDeleted, cancellationToken);

        if (!trackInPlaylist)
        {
            var nextPosition = likedSongsPlaylist.TrackCount + 1;

            dbContext.PlaylistTracks.Add(new PlaylistTrack
            {
                PlaylistId = likedSongsPlaylist.Id,
                TrackId = trackId,
                Position = nextPosition,
                AddedBy = userId,
                AddedAt = DateTime.UtcNow
            });

            likedSongsPlaylist.TrackCount += 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
