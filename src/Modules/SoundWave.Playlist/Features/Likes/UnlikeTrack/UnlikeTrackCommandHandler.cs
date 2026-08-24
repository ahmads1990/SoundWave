using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data;
using SoundWave.Playlist.Data.Entities;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Likes.UnlikeTrack;

/// <summary>
/// Handles unliking a track for the authenticated user and removing it from their system "Liked Songs" playlist.
/// </summary>
internal class UnlikeTrackCommandHandler(
    PlaylistDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<UnlikeTrackCommandHandler> logger)
    : IRequestHandler<UnlikeTrackCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        UnlikeTrackCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        await UnlikeTrackAsync(userId, request.TrackId, cancellationToken);

        logger.LogInformation("Track {TrackId} unliked by user {UserId}", request.TrackId, userId);
        return Result<PlaylistError, bool>.Success(true);
    }

    #region Private Methods

    private async Task UnlikeTrackAsync(
        Guid userId,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var likedTrack = await dbContext.LikedTracks
            .FirstOrDefaultAsync(lt => lt.UserId == userId && lt.TrackId == trackId, cancellationToken);

        if (likedTrack is not null)
        {
            dbContext.LikedTracks.Remove(likedTrack);
        }

        var likedSongsPlaylist = await dbContext.Playlists
            .FirstOrDefaultAsync(p => p.OwnerId == userId && p.IsSystem && !p.IsDeleted, cancellationToken);

        if (likedSongsPlaylist is not null)
        {
            var playlistTrack = await dbContext.PlaylistTracks
                .FirstOrDefaultAsync(pt => pt.PlaylistId == likedSongsPlaylist.Id && pt.TrackId == trackId && !pt.IsDeleted, cancellationToken);

            if (playlistTrack is not null)
            {
                var removedPosition = playlistTrack.Position;

                playlistTrack.IsDeleted = true;
                playlistTrack.UpdatedDate = DateTime.UtcNow;
                playlistTrack.UpdatedBy = userId;

                var subsequentTracks = await dbContext.PlaylistTracks
                    .Where(pt => pt.PlaylistId == likedSongsPlaylist.Id && pt.Position > removedPosition && !pt.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var track in subsequentTracks)
                {
                    track.Position -= 1;
                }

                likedSongsPlaylist.TrackCount = Math.Max(0, likedSongsPlaylist.TrackCount - 1);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
