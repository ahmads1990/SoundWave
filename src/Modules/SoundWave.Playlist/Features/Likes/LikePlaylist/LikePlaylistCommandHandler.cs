using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data;
using SoundWave.Playlist.Data.Entities;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Likes.LikePlaylist;

/// <summary>
/// Handles following/liking a public playlist and incrementing its follower count.
/// </summary>
internal class LikePlaylistCommandHandler(
    PlaylistDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<LikePlaylistCommandHandler> logger)
    : IRequestHandler<LikePlaylistCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        LikePlaylistCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var validation = await ValidateAsync(request.PlaylistId, userId, cancellationToken);
        if (!validation.IsSuccess)
            return Result<PlaylistError, bool>.Failure(validation.Error, validation.ErrorMessage);

        var playlist = validation.Data!;

        var alreadyLiked = await dbContext.LikedPlaylists
            .AnyAsync(lp => lp.UserId == userId && lp.PlaylistId == request.PlaylistId, cancellationToken);

        if (!alreadyLiked)
        {
            dbContext.LikedPlaylists.Add(new LikedPlaylist
            {
                UserId = userId,
                PlaylistId = request.PlaylistId,
                LikedAt = DateTime.UtcNow
            });

            playlist.FollowerCount += 1;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Playlist {PlaylistId} followed/liked by user {UserId}", request.PlaylistId, userId);
        return Result<PlaylistError, bool>.Success(true);
    }

    #region Private Methods

    private async Task<Result<PlaylistError, Data.Entities.Playlist>> ValidateAsync(
        Guid playlistId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var playlist = await dbContext.Playlists
            .FirstOrDefaultAsync(p => p.Id == playlistId && !p.IsDeleted, cancellationToken);

        if (playlist is null)
        {
            logger.LogWarning("Like playlist rejected — playlist {PlaylistId} not found", playlistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.PlaylistNotFound, "Playlist not found.");
        }

        if (playlist.Visibility == PlaylistVisibility.Private && playlist.OwnerId != userId)
        {
            logger.LogWarning("Like playlist rejected — playlist {PlaylistId} is private and caller is not owner", playlistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.PlaylistNotFound, "Playlist not found.");
        }

        return Result<PlaylistError, Data.Entities.Playlist>.Success(playlist);
    }

    #endregion
}
