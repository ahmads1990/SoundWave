using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data;
using SoundWave.Playlist.Data.Entities;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Likes.LikeAlbum;

/// <summary>
/// Handles saving an album to the authenticated user's library.
/// </summary>
internal class LikeAlbumCommandHandler(
    PlaylistDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<LikeAlbumCommandHandler> logger)
    : IRequestHandler<LikeAlbumCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        LikeAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var alreadyLiked = await dbContext.LikedAlbums
            .AnyAsync(la => la.UserId == userId && la.AlbumId == request.AlbumId, cancellationToken);

        if (!alreadyLiked)
        {
            dbContext.LikedAlbums.Add(new LikedAlbum
            {
                UserId = userId,
                AlbumId = request.AlbumId,
                LikedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Album {AlbumId} saved to library by user {UserId}", request.AlbumId, userId);
        return Result<PlaylistError, bool>.Success(true);
    }
}
