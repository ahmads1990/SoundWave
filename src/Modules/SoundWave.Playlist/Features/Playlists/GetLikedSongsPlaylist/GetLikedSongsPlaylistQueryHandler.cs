using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data;
using SoundWave.Playlist.Features.Playlists.GetPlaylist;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Features.Playlists.GetLikedSongsPlaylist;

/// <summary>
/// Handles retrieving the system "Liked Songs" playlist for the currently authenticated user.
/// </summary>
internal class GetLikedSongsPlaylistQueryHandler(
    PlaylistReadDbContext readDbContext,
    ICurrentUserService currentUserService,
    ILogger<GetLikedSongsPlaylistQueryHandler> logger)
    : IRequestHandler<GetLikedSongsPlaylistQuery, Result<PlaylistError, PlaylistDetailDto>>
{
    public async Task<Result<PlaylistError, PlaylistDetailDto>> Handle(
        GetLikedSongsPlaylistQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var playlist = await readDbContext.Playlists
            .FirstOrDefaultAsync(p => p.OwnerId == userId && p.IsSystem && !p.IsDeleted, cancellationToken);

        if (playlist is null)
        {
            logger.LogInformation("System Liked Songs playlist does not yet exist for user {UserId}; returning empty virtual playlist", userId);
            return Result<PlaylistError, PlaylistDetailDto>.Success(CreateEmptyLikedSongsDto(userId));
        }

        var tracks = await GetTracksAsync(playlist.Id, cancellationToken);
        logger.LogInformation("Retrieved system Liked Songs playlist for user {UserId} with {Count} tracks", userId, tracks.Count);

        return Result<PlaylistError, PlaylistDetailDto>.Success(ToDetailDto(playlist, tracks));
    }

    #region Private Methods

    private static PlaylistDetailDto CreateEmptyLikedSongsDto(Guid userId)
    {
        return new PlaylistDetailDto(
            Id: Guid.Empty,
            Title: Constants.LikedSongsPlaylistTitle,
            Description: "Your auto-generated liked songs playlist.",
            CoverImageUrl: null,
            OwnerId: userId,
            Visibility: PlaylistVisibility.Private,
            IsSystem: true,
            TrackCount: 0,
            TotalDurationSeconds: 0,
            FollowerCount: 0,
            IsLikedByCurrentUser: false,
            IsOwner: true,
            Tracks: []);
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
            false,
            true,
            tracks);
    }

    #endregion
}
