using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data;
using SoundWave.Playlist.Features.Playlists.ListPublicPlaylists;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Playlist.Features.Playlists.GetUserPublicPlaylists;

/// <summary>
/// Handles retrieving all public playlists created by a specific user or artist using Mapster projection.
/// </summary>
internal class GetUserPublicPlaylistsQueryHandler(
    PlaylistReadDbContext readDbContext,
    ILogger<GetUserPublicPlaylistsQueryHandler> logger)
    : IRequestHandler<GetUserPublicPlaylistsQuery, Result<PlaylistError, IReadOnlyList<PublicPlaylistSummaryDto>>>
{
    public async Task<Result<PlaylistError, IReadOnlyList<PublicPlaylistSummaryDto>>> Handle(
        GetUserPublicPlaylistsQuery request,
        CancellationToken cancellationToken)
    {
        var query = readDbContext.Playlists.AsQueryable();
        query = ApplyFilters(query, request.UserId);
        query = ApplySorting(query);

        var playlists = await query
            .ProjectToType<PublicPlaylistSummaryDto>()
            .ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {Count} public playlists for user {UserId}", playlists.Count, request.UserId);
        return Result<PlaylistError, IReadOnlyList<PublicPlaylistSummaryDto>>.Success(playlists);
    }

    #region Private Methods

    private static IQueryable<PlaylistEntity> ApplyFilters(
        IQueryable<PlaylistEntity> query,
        Guid userId)
    {
        return query.Where(p => p.OwnerId == userId
            && p.Visibility == PlaylistVisibility.Public
            && !p.IsDeleted
            && !p.IsSystem);
    }

    private static IQueryable<PlaylistEntity> ApplySorting(IQueryable<PlaylistEntity> query)
    {
        return query
            .OrderByDescending(p => p.FollowerCount)
            .ThenByDescending(p => p.CreatedDate);
    }

    #endregion
}
