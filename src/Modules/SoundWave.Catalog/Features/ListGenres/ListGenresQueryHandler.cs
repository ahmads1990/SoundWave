using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models.Responses;
using System.Text.Json;

namespace SoundWave.Catalog.Features.ListGenres;

/// <summary>
/// Handles retrieving paginated music genres and moods from the catalog with Redis caching.
/// </summary>
internal class ListGenresQueryHandler(
    CatalogReadDbContext dbContext,
    ICachingService cachingService,
    ILogger<ListGenresQueryHandler> logger)
    : IRequestHandler<ListGenresQuery, Result<CatalogError, PaginatedResponse<ListGenreDto>>>
{
    public async Task<Result<CatalogError, PaginatedResponse<ListGenreDto>>> Handle(
        ListGenresQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = Constants.Caching.GetListGenresKey(
            request.PageIndex, request.PageSize, request.Name,
            request.Type, request.OrderBy, request.SortDirection);

        var cachedData = await cachingService.GetAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedData))
        {
            logger.LogInformation("Cache hit for genres list (Key: {CacheKey})", cacheKey);
            return Result<CatalogError, PaginatedResponse<ListGenreDto>>.Success(
                JsonSerializer.Deserialize<PaginatedResponse<ListGenreDto>>(cachedData)!);
        }

        var response = await GetGenresAsync(request, cancellationToken);

        await cachingService.AddAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            TimeSpan.FromMinutes(Constants.Caching.GenresListTtlMinutes),
            cancellationToken);

        return Result<CatalogError, PaginatedResponse<ListGenreDto>>.Success(response);
    }

    #region Private Methods

    private async Task<PaginatedResponse<ListGenreDto>> GetGenresAsync(
        ListGenresQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Cache miss for genres list — querying database (PageIndex: {PageIndex}, PageSize: {PageSize})", request.PageIndex, request.PageSize);

        var query = dbContext.Genres.AsQueryable();
        query = ApplySearchFilters(query, request);
        query = ApplySorting(query, request);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new ListGenreDto(g.Id, g.Name, g.Type))
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<ListGenreDto>(items, totalCount, request.PageIndex, request.PageSize);
    }

    private static IQueryable<Genre> ApplySearchFilters(IQueryable<Genre> query, ListGenresQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(g => EF.Functions.Like(g.Name, $"%{request.Name}%"));
        }

        if (request.Type.HasValue)
        {
            query = query.Where(g => g.Type == request.Type.Value);
        }

        return query;
    }

    private static IQueryable<Genre> ApplySorting(IQueryable<Genre> query, ListGenresQuery request)
    {
        var isDescending = request.SortDirection == SortingDirection.Descending;

        return (request.OrderBy?.ToLower()) switch
        {
            "type" => isDescending ? query.OrderByDescending(g => g.Type).ThenBy(g => g.Name) : query.OrderBy(g => g.Type).ThenBy(g => g.Name),
            _ => isDescending ? query.OrderByDescending(g => g.Name) : query.OrderBy(g => g.Name)
        };
    }

    #endregion
}
