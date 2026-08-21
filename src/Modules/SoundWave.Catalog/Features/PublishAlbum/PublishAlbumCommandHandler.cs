using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.PublishAlbum;

/// <summary>
/// Handles publishing an album — sets IsPublished = true and stamps ReleaseDate.
/// Only the primary artist may publish. Album must have at least one track.
/// </summary>
internal class PublishAlbumCommandHandler(
    ICatalogRepository<Album> albumRepository,
    ICurrentUserService currentUserService,
    ILogger<PublishAlbumCommandHandler> logger)
    : IRequestHandler<PublishAlbumCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        PublishAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, Guid>.Failure(validation.Error, validation.ErrorMessage);

        var albumId = await PublishAndSaveAsync(request.AlbumId, cancellationToken);
        return Result<CatalogError, Guid>.Success(albumId);
    }

    #region Private Methods

    private async Task<Result<CatalogError, Guid>> ValidateAsync(
        PublishAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // Validation query using ICatalogRepository<Album>
        var albumValidation = await albumRepository.GetAll()
            .Where(a => a.Id == request.AlbumId)
            .Select(a => new
            {
                Exists = true,
                IsPrimaryArtist = a.AlbumArtists.Any(aa => aa.Order == 0 && aa.Artist.UserId == userId),
                IsPublished = a.IsPublished,
                TrackCount = a.Tracks.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (albumValidation is null)
        {
            logger.LogWarning("Publish album rejected — album {AlbumId} not found", request.AlbumId);
            return Result<CatalogError, Guid>.Failure(CatalogError.AlbumNotFound, $"Album '{request.AlbumId}' not found.");
        }

        if (!albumValidation.IsPrimaryArtist)
        {
            logger.LogWarning("Publish album rejected — user {UserId} is not the primary artist of album {AlbumId}", userId, request.AlbumId);
            return Result<CatalogError, Guid>.Failure(CatalogError.UnauthorizedAlbumAccess, "Only the primary artist can publish this album.");
        }

        if (albumValidation.IsPublished)
        {
            logger.LogWarning("Publish album rejected — album {AlbumId} is already published", request.AlbumId);
            return Result<CatalogError, Guid>.Failure(CatalogError.AlbumAlreadyPublished, "This album is already published.");
        }

        if (albumValidation.TrackCount == 0)
        {
            logger.LogWarning("Publish album rejected — album {AlbumId} has no tracks", request.AlbumId);
            return Result<CatalogError, Guid>.Failure(CatalogError.CannotPublishEmptyAlbum, "Cannot publish an album with no tracks.");
        }

        return Result<CatalogError, Guid>.Success(request.AlbumId);
    }

    private async Task<Guid> PublishAndSaveAsync(
        Guid albumId,
        CancellationToken cancellationToken)
    {
        var album = new Album
        {
            Id = albumId,
            IsPublished = true,
            ReleaseDate = DateTime.UtcNow
        };

        // Zero-read partial update via SaveInclude
        albumRepository.SaveInclude(album, nameof(Album.IsPublished), nameof(Album.ReleaseDate));
        await albumRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Album '{AlbumId}' published successfully", albumId);
        return album.Id;
    }

    #endregion
}
