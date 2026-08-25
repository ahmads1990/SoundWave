using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Playlists.GetPlaylist;

/// <summary>
/// Handles retrieving full details of a playlist including track list and authorization checks.
/// </summary>
internal class GetPlaylistQueryHandler(
    PlaylistReadDbContext readDbContext,
    ICurrentUserService currentUserService,
    ILogger<GetPlaylistQueryHandler> logger)
    : IRequestHandler<GetPlaylistQuery, Result<PlaylistError, PlaylistDetailDto>>
{
    public async Task<Result<PlaylistError, PlaylistDetailDto>> Handle(
        GetPlaylistQuery request,
        CancellationToken cancellationToken)
    {
        var playlist = await readDbContext.Playlists
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId && !p.IsDeleted, cancellationToken);

        if (playlist is null)
        {
            logger.LogWarning("Playlist {PlaylistId} was not found", request.PlaylistId);
            return Result<PlaylistError, PlaylistDetailDto>.Failure(PlaylistError.PlaylistNotFound);
        }

        var currentUserId = currentUserService.IsAuthenticated ? currentUserService.UserId : null;
        if (!await HasAccessAsync(playlist, currentUserId, cancellationToken))
        {
            logger.LogWarning("Access denied to private playlist {PlaylistId} for user {UserId}", playlist.Id, currentUserId);
            return Result<PlaylistError, PlaylistDetailDto>.Failure(PlaylistError.PlaylistNotFound);
        }

        var isLiked = await CheckIsLikedAsync(playlist.Id, currentUserId, cancellationToken);
        var tracks = await GetTracksAsync(playlist.Id, cancellationToken);
        var isOwner = currentUserId.HasValue && currentUserId.Value == playlist.OwnerId;

        return Result<PlaylistError, PlaylistDetailDto>.Success(ToDetailDto(playlist, isLiked, isOwner, tracks));
    }

    #region Private Methods

    private async Task<bool> HasAccessAsync(
        PlaylistEntity playlist,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        if (playlist.Visibility == PlaylistVisibility.Public)
            return true;

        if (!currentUserId.HasValue)
            return false;

        if (playlist.OwnerId == currentUserId.Value)
            return true;

        return await readDbContext.PlaylistCollaborators
            .AnyAsync(c => c.PlaylistId == playlist.Id && c.UserId == currentUserId.Value, cancellationToken);
    }

    private async Task<bool> CheckIsLikedAsync(
        Guid playlistId,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        if (!currentUserId.HasValue)
            return false;

        return await readDbContext.LikedPlaylists
            .AnyAsync(lp => lp.PlaylistId == playlistId && lp.UserId == currentUserId.Value, cancellationToken);
    }

    private async Task<IReadOnlyList<PlaylistTrackItemDto>> GetTracksAsync(
        Guid playlistId,
        CancellationToken cancellationToken)
    {
        return await readDbContext.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlistId && !pt.IsDeleted)
            .OrderBy(pt => pt.Position)
            .ProjectToType<PlaylistTrackItemDto>()
            .ToListAsync(cancellationToken);
    }

    private static PlaylistDetailDto ToDetailDto(
        PlaylistEntity playlist,
        bool isLiked,
        bool isOwner,
        IReadOnlyList<PlaylistTrackItemDto> tracks)
    {
        return new PlaylistDetailDto(
            playlist.Id,
            playlist.Title,
            playlist.Description,
            playlist.CoverImageUrl,
            playlist.OwnerId,
            playlist.Visibility,
            playlist.IsSystem,
            playlist.TrackCount,
            0,
            playlist.FollowerCount,
            isLiked,
            isOwner,
            tracks);
    }

    #endregion
}
