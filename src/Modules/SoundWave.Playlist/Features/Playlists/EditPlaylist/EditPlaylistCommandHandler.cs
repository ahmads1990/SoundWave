using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Playlists.EditPlaylist;

/// <summary>
/// Handles updating the metadata of an existing playlist.
/// </summary>
internal class EditPlaylistCommandHandler(
    IPlaylistRepository<Data.Entities.Playlist> playlistRepository,
    ICurrentUserService currentUserService,
    ILogger<EditPlaylistCommandHandler> logger)
    : IRequestHandler<EditPlaylistCommand, Result<PlaylistError, bool>>
{
    public async Task<Result<PlaylistError, bool>> Handle(
        EditPlaylistCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var validation = await ValidateAsync(request, userId, cancellationToken);
        if (!validation.IsSuccess)
            return Result<PlaylistError, bool>.Failure(validation.Error, validation.ErrorMessage);

        var playlist = validation.Data!;
        playlist.Title = request.Title.Trim();
        playlist.Description = request.Description?.Trim();
        playlist.Visibility = request.Visibility;

        playlistRepository.SaveInclude(
            playlist,
            nameof(Data.Entities.Playlist.Title),
            nameof(Data.Entities.Playlist.Description),
            nameof(Data.Entities.Playlist.Visibility));

        await playlistRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Playlist {PlaylistId} ('{Title}') updated by owner {UserId}", playlist.Id, playlist.Title, userId);

        return Result<PlaylistError, bool>.Success(true);
    }

    #region Private Methods

    private async Task<Result<PlaylistError, Data.Entities.Playlist>> ValidateAsync(
        EditPlaylistCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var playlist = await playlistRepository.GetAll()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (playlist is null)
        {
            logger.LogWarning("Edit playlist {PlaylistId} rejected — not found", request.Id);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.PlaylistNotFound, "Playlist not found.");
        }

        if (playlist.IsSystem)
        {
            logger.LogWarning("Edit playlist {PlaylistId} rejected — system playlist cannot be modified", request.Id);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.SystemPlaylistProtected, "System playlists cannot be modified.");
        }

        if (playlist.OwnerId != userId)
        {
            logger.LogWarning("Edit playlist {PlaylistId} rejected — user {UserId} is not the owner ({OwnerId})",
                request.Id, userId, playlist.OwnerId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.Unauthorized, "You do not have permission to edit this playlist.");
        }

        return Result<PlaylistError, Data.Entities.Playlist>.Success(playlist);
    }

    #endregion
}
