using System.Text.Json;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.ListPublicPlaylists;

/// <summary>
/// Handles paginated public playlist exploration with Redis caching, search filtering, and Mapster projection.
/// </summary>
internal class ListPublicPlaylistsQueryHandler(
    PlaylistReadDbContext readDbContext,
    ICachingService cachingService,
    ILogger<ListPublicPlaylistsQueryHandler> logger)
    : IRequestHandler<ListPublicPlaylistsQuery, Result<PlaylistError, PaginatedResponse<PublicPlaylistSummaryDto>>>
{
    public async Task<Result<PlaylistError, PaginatedResponse<PublicPlaylistSummaryDto>>> Handle(
        ListPublicPlaylistsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = Constants.Caching.GetPublicPlaylistsKey(
            request.PageIndex,
            request.PageSize,
            request.SearchTerm,
            request.OrderBy,
            request.SortDirection);

        var cached = await cachingService.GetAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            logger.LogInformation("Cache hit for public playlists (Key: {CacheKey})", cacheKey);
            var cachedResponse = JsonSerializer.Deserialize<PaginatedResponse<PublicPlaylistSummaryDto>>(cached);
            if (cachedResponse is not null)
                return Result<PlaylistError, PaginatedResponse<PublicPlaylistSummaryDto>>.Success(cachedResponse);
        }

        var response = await GetPublicPlaylistsAsync(request, cancellationToken);

        await cachingService.AddAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            TimeSpan.FromMinutes(Constants.Caching.PublicPlaylistsListTtlMinutes),
            cancellationToken);

        return Result<PlaylistError, PaginatedResponse<PublicPlaylistSummaryDto>>.Success(response);
    }

    #region Private Methods

    private async Task<PaginatedResponse<PublicPlaylistSummaryDto>> GetPublicPlaylistsAsync(
        ListPublicPlaylistsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying public playlists from database (PageIndex: {PageIndex}, Query: {Query})", request.PageIndex, request.SearchTerm);

        var query = readDbContext.Playlists.AsQueryable();
        query = ApplyFilters(query, request.SearchTerm);
        query = ApplySorting(query, request.OrderBy, request.SortDirection);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ProjectToType<PublicPlaylistSummaryDto>()
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<PublicPlaylistSummaryDto>(items, totalCount, request.PageIndex, request.PageSize);
    }

    private static IQueryable<PlaylistEntity> ApplyFilters(
        IQueryable<PlaylistEntity> query,
        string? searchTerm)
    {
        query = query.Where(p => p.Visibility == PlaylistVisibility.Public && !p.IsDeleted && !p.IsSystem);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(p => EF.Functions.Like(p.Title, $"%{term}%")
                || (p.Description != null && EF.Functions.Like(p.Description, $"%{term}%")));
        }

        return query;
    }

    private static IQueryable<PlaylistEntity> ApplySorting(
        IQueryable<PlaylistEntity> query,
        string? orderBy,
        SortingDirection sortDirection)
    {
        var isDesc = sortDirection == SortingDirection.Descending;

        if (string.Equals(orderBy, nameof(PlaylistEntity.CreatedDate), StringComparison.OrdinalIgnoreCase))
            return isDesc ? query.OrderByDescending(p => p.CreatedDate) : query.OrderBy(p => p.CreatedDate);

        if (string.Equals(orderBy, nameof(PlaylistEntity.Title), StringComparison.OrdinalIgnoreCase))
            return isDesc ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title);

        // Default sorting: FollowerCount (Popularity)
        return isDesc
            ? query.OrderByDescending(p => p.FollowerCount).ThenByDescending(p => p.CreatedDate)
            : query.OrderBy(p => p.FollowerCount).ThenBy(p => p.CreatedDate);
    }

    #endregion
}
