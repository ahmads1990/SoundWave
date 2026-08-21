using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.Catalog.Features.ValidateGenresExist;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Features.CreateAlbum;

/// <summary>
/// Handles creating a new album by an authenticated artist.
/// </summary>
internal class CreateAlbumCommandHandler(
    ICatalogRepository<Album> albumRepository,
    ICatalogRepository<Artist> artistRepository,
    ISender sender,
    ICurrentUserService currentUserService,
    ILogger<CreateAlbumCommandHandler> logger)
    : IRequestHandler<CreateAlbumCommand, Result<CatalogError, Guid>>
{
    public async Task<Result<CatalogError, Guid>> Handle(
        CreateAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, Guid>.Failure(validation.Error, validation.ErrorMessage);

        var artistId = validation.Data;
        var album = await CreateAndSaveAlbumAsync(request, artistId, cancellationToken);
        return Result<CatalogError, Guid>.Success(album.Id);
    }

    #region Private Methods

    private async Task<Result<CatalogError, Guid>> ValidateAsync(
        CreateAlbumCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;
        var artistId = await artistRepository.GetAll()
            .Where(a => a.UserId == userId)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (artistId is null)
        {
            logger.LogWarning("Create album rejected — artist profile not found for user {UserId}", userId);
            return Result<CatalogError, Guid>.Failure(CatalogError.ArtistNotFound, "Artist profile not found for current user.");
        }

        if (request.GenreIds is { Count: > 0 })
        {
            var allGenresExist = await sender.Send(new ValidateGenresExistQuery(request.GenreIds), cancellationToken);
            if (!allGenresExist)
            {
                logger.LogWarning("Create album rejected — one or more genre IDs are invalid");
                return Result<CatalogError, Guid>.Failure(CatalogError.InvalidGenreId, "One or more specified genre IDs do not exist.");
            }
        }

        return Result<CatalogError, Guid>.Success(artistId.Value);
    }

    private async Task<Album> CreateAndSaveAlbumAsync(
        CreateAlbumCommand request,
        Guid artistId,
        CancellationToken cancellationToken)
    {
        var album = new Album
        {
            Title = request.Title,
            AlbumType = request.AlbumType,
            ReleaseDate = request.ReleaseDate,
            CoverImageUrl = request.CoverImageUrl,
            Description = request.Description,
            IsPublished = false,
            TrackCount = 0
        };

        // Add primary artist (Order = 0)
        album.AlbumArtists.Add(new AlbumArtist
        {
            ArtistId = artistId,
            Order = 0
        });

        // Add featured/collaborating artists
        if (request.FeaturedArtistIds is { Count: > 0 })
        {
            var order = 1;
            foreach (var featuredId in request.FeaturedArtistIds.Distinct().Where(id => id != artistId))
            {
                album.AlbumArtists.Add(new AlbumArtist
                {
                    ArtistId = featuredId,
                    Order = order++
                });
            }
        }

        // Add genres
        if (request.GenreIds is { Count: > 0 })
        {
            foreach (var genreId in request.GenreIds.Distinct())
            {
                album.AlbumGenres.Add(new AlbumGenre { GenreId = genreId });
            }
        }

        await albumRepository.Add(album, cancellationToken);
        await albumRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Album '{AlbumTitle}' ({AlbumId}) created by Artist {ArtistId}", album.Title, album.Id, artistId);
        return album;
    }

    #endregion
}
