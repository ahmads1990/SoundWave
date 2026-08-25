using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Data;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Playlists.GetMyPlaylistsSimple;

/// <summary>
/// Handles retrieving a lightweight list of the current user's editable playlists with optional search.
/// </summary>
internal class GetMyPlaylistsSimpleQueryHandler(
    PlaylistReadDbContext readDbContext,
    ICurrentUserService currentUserService,
    ILogger<GetMyPlaylistsSimpleQueryHandler> logger)
    : IRequestHandler<GetMyPlaylistsSimpleQuery, Result<PlaylistError, IReadOnlyList<SimplePlaylistDto>>>
{
    public async Task<Result<PlaylistError, IReadOnlyList<SimplePlaylistDto>>> Handle(
        GetMyPlaylistsSimpleQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var query = readDbContext.Playlists.AsQueryable();
        query = ApplyFilters(query, userId, request.SearchTerm);
        query = ApplySorting(query);

        var playlists = await ProjectToDto(query, request.TrackId)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {Count} simple playlists for user {UserId}", playlists.Count, userId);
        return Result<PlaylistError, IReadOnlyList<SimplePlaylistDto>>.Success(playlists);
    }

    #region Private Methods

    private static IQueryable<PlaylistEntity> ApplyFilters(
        IQueryable<PlaylistEntity> query,
        Guid userId,
        string? searchTerm)
    {
        query = query.Where(p => p.OwnerId == userId && !p.IsSystem && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(p => EF.Functions.Like(p.Title, $"%{term}%"));
        }

        return query;
    }

    private static IQueryable<PlaylistEntity> ApplySorting(IQueryable<PlaylistEntity> query)
        => query.OrderByDescending(p => p.CreatedDate);

    private IQueryable<SimplePlaylistDto> ProjectToDto(
        IQueryable<PlaylistEntity> query,
        Guid? trackId)
    {
        return query.Select(p => new SimplePlaylistDto(
            p.Id,
            p.Title,
            p.CoverImageUrl,
            p.TrackCount,
            trackId.HasValue && readDbContext.PlaylistTracks
                .Any(pt => pt.PlaylistId == p.Id && pt.TrackId == trackId.Value && !pt.IsDeleted)));
    }

    #endregion
}
