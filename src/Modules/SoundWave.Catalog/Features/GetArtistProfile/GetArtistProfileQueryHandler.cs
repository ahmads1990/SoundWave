using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using System.Text.Json;

namespace SoundWave.Catalog.Features.GetArtistProfile;

/// <summary>
/// Handles retrieving an artist's full public profile, top tracks, and published discography with Redis caching.
/// </summary>
internal class GetArtistProfileQueryHandler(
    ICatalogReadRepository<Artist> artistReadRepository,
    CatalogReadDbContext dbContext,
    ICachingService cachingService,
    ILogger<GetArtistProfileQueryHandler> logger)
    : IRequestHandler<GetArtistProfileQuery, Result<CatalogError, ArtistProfileDto>>
{
    public async Task<Result<CatalogError, ArtistProfileDto>> Handle(
        GetArtistProfileQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = Constants.Caching.GetArtistProfileKey(request.ArtistId);

        var cachedProfile = await TryGetFromCacheAsync(cacheKey, cancellationToken);
        if (cachedProfile is not null)
            return Result<CatalogError, ArtistProfileDto>.Success(cachedProfile);

        return await FetchAndCacheArtistProfileAsync(request.ArtistId, cacheKey, cancellationToken);
    }

    #region Private Methods

    private async Task<ArtistProfileDto?> TryGetFromCacheAsync(
        string cacheKey,
        CancellationToken cancellationToken)
    {
        var cachedData = await cachingService.GetAsync(cacheKey, cancellationToken);
        if (string.IsNullOrEmpty(cachedData))
            return null;

        logger.LogInformation("Cache hit for artist profile (Key: {CacheKey})", cacheKey);
        return JsonSerializer.Deserialize<ArtistProfileDto>(cachedData);
    }

    private async Task<Result<CatalogError, ArtistProfileDto>> FetchAndCacheArtistProfileAsync(
        Guid artistId,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Cache miss for artist profile — querying database (ArtistId: {ArtistId})", artistId);

        var artist = await artistReadRepository.GetAll()
            .FirstOrDefaultAsync(a => a.Id == artistId, cancellationToken);

        if (artist is null)
        {
            logger.LogWarning("Artist profile not found for ID {ArtistId}", artistId);
            return Result<CatalogError, ArtistProfileDto>.Failure(CatalogError.ArtistNotFound, $"Artist with ID '{artistId}' was not found.");
        }

        // Top 10 tracks by play count
        var topTracks = await dbContext.TrackArtists
            .AsNoTracking()
            .Where(ta => ta.ArtistId == artistId)
            .Select(ta => ta.Track)
            .OrderByDescending(t => t.PlayCount)
            .Take(10)
            .Select(t => new ArtistTopTrackDto(t.Id, t.Title, t.DurationSeconds, t.TrackNumber, t.PlayCount, t.LikeCount, t.AlbumId, t.Album.Title, t.Album.CoverImageUrl))
            .ToListAsync(cancellationToken);

        // Published albums ordered by release date
        var albums = await dbContext.AlbumArtists
            .AsNoTracking()
            .Where(aa => aa.ArtistId == artistId && aa.Album.IsPublished)
            .Select(aa => aa.Album)
            .OrderByDescending(al => al.ReleaseDate)
            .Select(al => new ArtistAlbumDto(al.Id, al.Title, al.AlbumType, al.ReleaseDate, al.CoverImageUrl, al.TrackCount))
            .ToListAsync(cancellationToken);

        var profileDto = new ArtistProfileDto(artist.Id, artist.UserId, artist.StageName, artist.Bio, artist.FollowerCount, artist.MonthlyListeners, artist.TotalStreams, artist.ApprovedAt, topTracks, albums);

        var serialized = JsonSerializer.Serialize(profileDto);
        var ttl = TimeSpan.FromMinutes(Constants.Caching.ArtistProfileTtlMinutes);
        await cachingService.AddAsync(cacheKey, serialized, ttl, cancellationToken);

        return Result<CatalogError, ArtistProfileDto>.Success(profileDto);
    }

    #endregion
}
