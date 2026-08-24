using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Likes.UnlikePlaylist;

/// <summary>
/// Handles unfollowing/unliking a playlist and decrementing its follower count.
/// </summary>
internal class UnlikePlaylistCommandHandler(
    PlaylistDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<UnlikePlaylistCommandHandler> logger)
    : IRequestHandler<UnlikePlaylistCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        UnlikePlaylistCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var likedPlaylist = await dbContext.LikedPlaylists
            .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.PlaylistId == request.PlaylistId, cancellationToken);

        if (likedPlaylist is not null)
        {
            dbContext.LikedPlaylists.Remove(likedPlaylist);

            var playlist = await dbContext.Playlists
                .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, cancellationToken);

            if (playlist is not null)
            {
                playlist.FollowerCount = Math.Max(0, playlist.FollowerCount - 1);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Playlist {PlaylistId} unfollowed/unliked by user {UserId}", request.PlaylistId, userId);
        return Result<PlaylistError, bool>.Success(true);
    }
}
