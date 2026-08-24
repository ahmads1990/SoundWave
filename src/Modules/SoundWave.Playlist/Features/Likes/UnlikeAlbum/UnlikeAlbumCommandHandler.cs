using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Likes.UnlikeAlbum;

/// <summary>
/// Handles removing/unsaving an album from the authenticated user's library.
/// </summary>
internal class UnlikeAlbumCommandHandler(
    PlaylistDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<UnlikeAlbumCommandHandler> logger)
    : IRequestHandler<UnlikeAlbumCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        UnlikeAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var likedAlbum = await dbContext.LikedAlbums
            .FirstOrDefaultAsync(la => la.UserId == userId && la.AlbumId == request.AlbumId, cancellationToken);

        if (likedAlbum is not null)
        {
            dbContext.LikedAlbums.Remove(likedAlbum);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Album {AlbumId} removed from library by user {UserId}", request.AlbumId, userId);
        return Result<PlaylistError, bool>.Success(true);
    }
}
