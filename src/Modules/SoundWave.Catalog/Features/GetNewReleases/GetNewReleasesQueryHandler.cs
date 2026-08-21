using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models.Responses;
using System.Text.Json;

namespace SoundWave.Catalog.Features.GetNewReleases;

/// <summary>
/// Handles retrieving paginated newly released published albums with Redis caching and filters.
/// </summary>
internal class GetNewReleasesQueryHandler(
    ICatalogReadRepository<Album> albumReadRepository,
    ICachingService cachingService,
    ILogger<GetNewReleasesQueryHandler> logger)
    : IRequestHandler<GetNewReleasesQuery, Result<CatalogError, PaginatedResponse<AlbumSummaryDto>>>
{
    public async Task<Result<CatalogError, PaginatedResponse<AlbumSummaryDto>>> Handle(
        GetNewReleasesQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var pageIndex = Math.Max(0, request.PageIndex);

        var cacheKey = Constants.Caching.GetNewReleasesKey(pageIndex, pageSize, request.GenreId, request.AlbumType, request.DaysOld);

        var cached = await cachingService.GetAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            logger.LogInformation("Cache hit for new releases (Key: {CacheKey})", cacheKey);
            return Result<CatalogError, PaginatedResponse<AlbumSummaryDto>>.Success(
                JsonSerializer.Deserialize<PaginatedResponse<AlbumSummaryDto>>(cached)!);
        }

        var response = await GetNewReleasesAsync(pageIndex, pageSize, request, cancellationToken);

        await cachingService.AddAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            TimeSpan.FromMinutes(Constants.Caching.NewReleasesTtlMinutes),
            cancellationToken);

        return Result<CatalogError, PaginatedResponse<AlbumSummaryDto>>.Success(response);
    }

    #region Private Methods

    private async Task<PaginatedResponse<AlbumSummaryDto>> GetNewReleasesAsync(
        int pageIndex,
        int pageSize,
        GetNewReleasesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Cache miss for new releases — querying database (PageIndex: {PageIndex}, PageSize: {PageSize})", pageIndex, pageSize);

        var query = albumReadRepository.GetAll()
            .Where(a => a.IsPublished);

        if (request.GenreId.HasValue)
        {
            query = query.Where(a => a.AlbumGenres.Any(ag => ag.GenreId == request.GenreId.Value));
        }

        if (request.AlbumType.HasValue)
        {
            query = query.Where(a => a.AlbumType == request.AlbumType.Value);
        }

        if (request.DaysOld.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-request.DaysOld.Value);
            query = query.Where(a => a.ReleaseDate >= cutoffDate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.ReleaseDate)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(a => new AlbumSummaryDto(
                a.Id,
                a.Title,
                a.AlbumType,
                a.ReleaseDate,
                a.CoverImageUrl,
                a.TrackCount,
                a.AlbumArtists
                    .OrderBy(aa => aa.Order)
                    .Select(aa => new AlbumSummaryArtistDto(aa.ArtistId, aa.Artist.StageName, aa.Order))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<AlbumSummaryDto>(items, totalCount, pageIndex, pageSize);
    }

    #endregion
}
