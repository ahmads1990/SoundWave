using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Playlists.DeletePlaylist;

/// <summary>
/// Handles soft-deleting a playlist.
/// </summary>
internal class DeletePlaylistCommandHandler(
    IPlaylistRepository<Data.Entities.Playlist> playlistRepository,
    ICurrentUserService currentUserService,
    ILogger<DeletePlaylistCommandHandler> logger)
    : IRequestHandler<DeletePlaylistCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        DeletePlaylistCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var validation = await ValidateAsync(request.Id, userId, cancellationToken);
        if (!validation.IsSuccess)
            return Result<PlaylistError, bool>.Failure(validation.Error, validation.ErrorMessage);

        var playlist = validation.Data!;
        playlistRepository.SoftDelete(playlist);
        await playlistRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Playlist {PlaylistId} ('{Title}') soft-deleted by owner {UserId}", playlist.Id, playlist.Title, userId);

        return Result<PlaylistError, bool>.Success(true);
    }

    #region Private Methods

    private async Task<Result<PlaylistError, Data.Entities.Playlist>> ValidateAsync(
        Guid playlistId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var playlist = await playlistRepository.GetAll()
            .FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken);

        if (playlist is null)
        {
            logger.LogWarning("Delete playlist {PlaylistId} rejected — not found", playlistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.PlaylistNotFound, "Playlist not found.");
        }

        if (playlist.IsSystem)
        {
            logger.LogWarning("Delete playlist {PlaylistId} rejected — system playlist cannot be deleted", playlistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.SystemPlaylistProtected, "System playlists like 'Liked Songs' cannot be deleted.");
        }

        if (playlist.OwnerId != userId)
        {
            logger.LogWarning("Delete playlist {PlaylistId} rejected — user {UserId} is not the owner ({OwnerId})",
                playlistId, userId, playlist.OwnerId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.Unauthorized, "You do not have permission to delete this playlist.");
        }

        return Result<PlaylistError, Data.Entities.Playlist>.Success(playlist);
    }

    #endregion
}
