using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.Albums.EditAlbum;

/// <summary>
/// Handles updating an album's metadata, genres, and featured artists.
/// Only the primary artist (Order = 0) of the album may edit it.
/// </summary>
internal class EditAlbumCommandHandler(
    ICatalogRepository<Album> albumRepository,
    ISender sender,
    ICurrentUserService currentUserService,
    ILogger<EditAlbumCommandHandler> logger)
    : IRequestHandler<EditAlbumCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        EditAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, Guid>.Failure(validation.Error, validation.ErrorMessage);

        await UpdateAlbumAsync(request, cancellationToken);
        return Result<CatalogError, Guid>.Success(request.AlbumId);
    }

    #region Private Methods

    private async Task<Result<CatalogError, Guid>> ValidateAsync(
        EditAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var albumValidation = await albumRepository.GetAll()
            .Where(a => a.Id == request.AlbumId)
            .Select(a => new
            {
                Exists = true,
                IsPrimaryArtist = a.AlbumArtists.Any(aa => aa.Order == 0 && aa.Artist.UserId == userId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (albumValidation is null)
        {
            logger.LogWarning("Edit album rejected — album {AlbumId} not found", request.AlbumId);
            return Result<CatalogError, Guid>.Failure(CatalogError.AlbumNotFound, $"Album '{request.AlbumId}' not found.");
        }

        if (!albumValidation.IsPrimaryArtist)
        {
            logger.LogWarning("Edit album rejected — user {UserId} is not the primary artist of album {AlbumId}", userId, request.AlbumId);
            return Result<CatalogError, Guid>.Failure(CatalogError.UnauthorizedAlbumAccess, "Only the primary artist can edit this album.");
        }

        if (request.GenreIds is { Count: > 0 })
        {
            var allGenresExist = await sender.Send(new ValidateGenresExistQuery(request.GenreIds), cancellationToken);
            if (!allGenresExist)
            {
                logger.LogWarning("Edit album rejected — one or more genre IDs are invalid");
                return Result<CatalogError, Guid>.Failure(CatalogError.InvalidGenreId, "One or more specified genre IDs do not exist.");
            }
        }

        return Result<CatalogError, Guid>.Success(request.AlbumId);
    }

    private async Task UpdateAlbumAsync(
        EditAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var album = await albumRepository.GetAll()
            .Include(a => a.AlbumArtists)
            .Include(a => a.AlbumGenres)
            .FirstAsync(a => a.Id == request.AlbumId, cancellationToken);

        album.Title = request.Title;
        album.AlbumType = request.AlbumType;
        album.ReleaseDate = request.ReleaseDate;
        album.CoverImageUrl = request.CoverImageUrl;
        album.Description = request.Description;

        // Sync AlbumGenres
        album.AlbumGenres.Clear();
        if (request.GenreIds is { Count: > 0 })
        {
            foreach (var genreId in request.GenreIds.Distinct())
            {
                album.AlbumGenres.Add(new AlbumGenre { GenreId = genreId });
            }
        }

        // Sync AlbumArtists — keep primary artist (Order = 0), replace featured artists
        var primaryArtist = album.AlbumArtists.FirstOrDefault(aa => aa.Order == 0);
        album.AlbumArtists.Clear();
        if (primaryArtist is not null)
        {
            album.AlbumArtists.Add(primaryArtist);
        }

        if (request.FeaturedArtistIds is { Count: > 0 })
        {
            var order = 1;
            foreach (var featuredId in request.FeaturedArtistIds.Distinct().Where(id => primaryArtist == null || id != primaryArtist.ArtistId))
            {
                album.AlbumArtists.Add(new AlbumArtist { ArtistId = featuredId, Order = order++ });
            }
        }

        await albumRepository.SaveChanges(cancellationToken);
        logger.LogInformation("Album '{AlbumTitle}' ({AlbumId}) metadata updated", album.Title, album.Id);
    }

    #endregion
}
