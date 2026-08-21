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

namespace SoundWave.Catalog.Features.ListAlbums;

/// <summary>
/// Handles paginated album listing with filtering, sorting, and Redis caching.
/// </summary>
internal class ListAlbumsQueryHandler(
    ICatalogReadRepository<Album> albumReadRepository,
    ICachingService cachingService,
    ILogger<ListAlbumsQueryHandler> logger)
    : IRequestHandler<ListAlbumsQuery, Result<CatalogError, PaginatedResponse<AlbumSummaryListDto>>>
{
    public async Task<Result<CatalogError, PaginatedResponse<AlbumSummaryListDto>>> Handle(
        ListAlbumsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = Constants.Caching.GetListAlbumsKey(
            request.PageIndex, request.PageSize, request.Title, request.GenreId,
            request.ArtistId, request.IsPublished, request.AlbumType,
            request.OrderBy, request.SortDirection);

        var cached = await cachingService.GetAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            logger.LogInformation("Cache hit for album list (Key: {CacheKey})", cacheKey);
            return Result<CatalogError, PaginatedResponse<AlbumSummaryListDto>>.Success(
                JsonSerializer.Deserialize<PaginatedResponse<AlbumSummaryListDto>>(cached)!);
        }

        var response = await GetAlbumsAsync(request, cancellationToken);

        await cachingService.AddAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            TimeSpan.FromMinutes(Constants.Caching.AlbumsListTtlMinutes),
            cancellationToken);

        return Result<CatalogError, PaginatedResponse<AlbumSummaryListDto>>.Success(response);
    }

    #region Private Methods

    private async Task<PaginatedResponse<AlbumSummaryListDto>> GetAlbumsAsync(
        ListAlbumsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Cache miss for album list — querying database (PageIndex: {PageIndex})", request.PageIndex);

        var query = albumReadRepository.GetAll();
        query = ApplyFilters(query, request);
        query = ApplySorting(query, request);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AlbumSummaryListDto(
                a.Id,
                a.Title,
                a.AlbumType,
                a.IsPublished,
                a.ReleaseDate,
                a.CoverImageUrl,
                a.TrackCount,
                a.AlbumArtists
                    .OrderBy(aa => aa.Order)
                    .Select(aa => new AlbumSummaryListArtistDto(aa.ArtistId, aa.Artist.StageName, aa.Order))
                    .ToList(),
                a.AlbumGenres
                    .Select(ag => new AlbumSummaryListGenreDto(ag.GenreId, ag.Genre.Name))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<AlbumSummaryListDto>(items, totalCount, request.PageIndex, request.PageSize);
    }

    private static IQueryable<Album> ApplyFilters(IQueryable<Album> query, ListAlbumsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Title))
            query = query.Where(a => EF.Functions.Like(a.Title, $"%{request.Title}%"));

        if (request.GenreId.HasValue)
            query = query.Where(a => a.AlbumGenres.Any(ag => ag.GenreId == request.GenreId.Value));

        if (request.ArtistId.HasValue)
            query = query.Where(a => a.AlbumArtists.Any(aa => aa.ArtistId == request.ArtistId.Value));

        if (request.IsPublished.HasValue)
            query = query.Where(a => a.IsPublished == request.IsPublished.Value);

        if (request.AlbumType.HasValue)
            query = query.Where(a => a.AlbumType == request.AlbumType.Value);

        return query;
    }

    private static IQueryable<Album> ApplySorting(IQueryable<Album> query, ListAlbumsQuery request)
    {
        var isDescending = request.SortDirection == SortingDirection.Descending;

        return request.OrderBy?.ToLower() switch
        {
            "releasedate" => isDescending
                ? query.OrderByDescending(a => a.ReleaseDate)
                : query.OrderBy(a => a.ReleaseDate),
            "trackcount" => isDescending
                ? query.OrderByDescending(a => a.TrackCount)
                : query.OrderBy(a => a.TrackCount),
            _ => isDescending
                ? query.OrderByDescending(a => a.Title)
                : query.OrderBy(a => a.Title)
        };
    }

    #endregion
}
