using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models.Responses;
using SoundWave.SharedKernel.Storage.Services;

namespace SoundWave.Playlist.Features.Playlists.UploadPlaylistCover;

/// <summary>
/// Handles validating playlist ownership and uploading its cover image artwork.
/// </summary>
internal class UploadPlaylistCoverCommandHandler(
    IPlaylistRepository<Data.Entities.Playlist> playlistRepository,
    IImageUploadService imageUploadService,
    ICurrentUserService currentUserService,
    ILogger<UploadPlaylistCoverCommandHandler> logger)
    : IRequestHandler<UploadPlaylistCoverCommand, Result<PlaylistError, UploadImageResponse>>
{
    public async Task<Result<PlaylistError, UploadImageResponse>> Handle(
        UploadPlaylistCoverCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<PlaylistError, UploadImageResponse>.Failure(validation.Error, validation.ErrorMessage);

        var playlist = validation.Data!;
        var previousCoverUrl = playlist.CoverImageUrl;

        var uploadResult = await imageUploadService.ReplaceImageAsync(
            request.Content,
            request.FileName,
            FileType.PlaylistCover,
            previousCoverUrl,
            cancellationToken: cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            logger.LogWarning("Cover upload failed for playlist {PlaylistId}: {Reason}", request.PlaylistId, uploadResult.ErrorMessage);
            return Result<PlaylistError, UploadImageResponse>.Failure(PlaylistError.FileValidationFailed, uploadResult.ErrorMessage);
        }

        playlist.CoverImageUrl = uploadResult.Data!.PublicUrl;
        playlistRepository.SaveInclude(playlist, nameof(Data.Entities.Playlist.CoverImageUrl));
        await playlistRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Cover artwork updated successfully for playlist {PlaylistId}", request.PlaylistId);

        return Result<PlaylistError, UploadImageResponse>.Success(
            new UploadImageResponse(uploadResult.Data.FileId, uploadResult.Data.PublicUrl, uploadResult.Data.SizeBytes));
    }

    #region Private Methods

    private async Task<Result<PlaylistError, Data.Entities.Playlist>> ValidateAsync(
        UploadPlaylistCoverCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var playlist = await playlistRepository.GetAll()
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId && !p.IsDeleted, cancellationToken);

        if (playlist is null)
        {
            logger.LogWarning("Upload playlist cover rejected — playlist {PlaylistId} not found", request.PlaylistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.PlaylistNotFound, "Playlist not found.");
        }

        if (playlist.IsSystem)
        {
            logger.LogWarning("Upload playlist cover rejected — system playlist {PlaylistId} cannot be modified", request.PlaylistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.SystemPlaylistProtected, "System playlists cannot be modified.");
        }

        if (playlist.OwnerId != userId)
        {
            logger.LogWarning("Upload playlist cover rejected — user {UserId} is not owner of playlist {PlaylistId}",
                userId, request.PlaylistId);
            return Result<PlaylistError, Data.Entities.Playlist>.Failure(PlaylistError.Unauthorized, "You do not have permission to edit this playlist.");
        }

        return Result<PlaylistError, Data.Entities.Playlist>.Success(playlist);
    }

    #endregion
}
