using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models.Responses;
using SoundWave.SharedKernel.Storage.Services;

namespace SoundWave.Catalog.Features.Albums.UploadAlbumCover;

/// <summary>
/// Handles validating ownership and uploading cover artwork for an album.
/// </summary>
internal class UploadAlbumCoverCommandHandler(
    ICatalogRepository<Album> albumRepository,
    IImageUploadService imageUploadService,
    ICurrentUserService currentUserService,
    ILogger<UploadAlbumCoverCommandHandler> logger)
    : IRequestHandler<UploadAlbumCoverCommand, Result<CatalogError, UploadImageResponse>>
{
    public async Task<Result<CatalogError, UploadImageResponse>> Handle(
        UploadAlbumCoverCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, UploadImageResponse>.Failure(validation.Error, validation.ErrorMessage);

        var album = validation.Data!;
        var previousCoverUrl = album.CoverImageUrl;

        var uploadResult = await imageUploadService.ReplaceImageAsync(
            request.Content,
            request.FileName,
            FileType.AlbumCover,
            previousCoverUrl,
            cancellationToken: cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            logger.LogWarning("Cover upload failed for album {AlbumId}: {Reason}", request.AlbumId, uploadResult.ErrorMessage);
            return Result<CatalogError, UploadImageResponse>.Failure(CatalogError.FileValidationFailed, uploadResult.ErrorMessage);
        }

        album.CoverImageUrl = uploadResult.Data!.PublicUrl;
        albumRepository.SaveInclude(album, nameof(Album.CoverImageUrl));
        await albumRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Cover artwork updated successfully for album {AlbumId}", request.AlbumId);

        return Result<CatalogError, UploadImageResponse>.Success(
            new UploadImageResponse(uploadResult.Data.FileId, uploadResult.Data.PublicUrl, uploadResult.Data.SizeBytes));
    }

    #region Private Methods

    private async Task<Result<CatalogError, Album>> ValidateAsync(
        UploadAlbumCoverCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var album = await albumRepository.GetAll()
            .Include(a => a.AlbumArtists)
            .ThenInclude(aa => aa.Artist)
            .FirstOrDefaultAsync(a => a.Id == request.AlbumId && !a.IsDeleted, cancellationToken);

        if (album is null)
        {
            logger.LogWarning("Upload album cover rejected — album {AlbumId} not found", request.AlbumId);
            return Result<CatalogError, Album>.Failure(CatalogError.AlbumNotFound, $"Album '{request.AlbumId}' not found.");
        }

        var isPrimaryArtist = album.AlbumArtists.Any(aa => aa.Order == 0 && aa.Artist.UserId == userId);
        if (!isPrimaryArtist)
        {
            logger.LogWarning("Upload album cover rejected — user {UserId} is not primary artist of album {AlbumId}",
                userId, request.AlbumId);
            return Result<CatalogError, Album>.Failure(CatalogError.UnauthorizedAlbumAccess, "Only the primary artist can update this album's cover.");
        }

        return Result<CatalogError, Album>.Success(album);
    }

    #endregion
}
