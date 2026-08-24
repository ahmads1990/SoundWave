using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Likes.LikeTrack;
using SoundWave.Playlist.Features.Likes.UnlikeTrack;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Likes;

public class LikeTrackTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<LikeTrackCommandHandler>> _likeLoggerMock = new();
    private readonly Mock<ILogger<UnlikeTrackCommandHandler>> _unlikeLoggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private LikeTrackCommandHandler BuildLikeHandler()
    {
        return new(DbContext, _currentUserMock.Object, _likeLoggerMock.Object);
    }

    private UnlikeTrackCommandHandler BuildUnlikeHandler()
    {
        return new(DbContext, _currentUserMock.Object, _unlikeLoggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    #region LikeTrack Tests

    [Fact]
    public async Task LikeTrack_ShouldAddLikedTrack_AndCreateSystemLikedSongsPlaylistWithTrack()
    {
        var userId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        SetupUser(userId);

        var command = new LikeTrackCommand(trackId);
        var result = await BuildLikeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var likedTrack = await DbContext.LikedTracks
            .FirstOrDefaultAsync(lt => lt.UserId == userId && lt.TrackId == trackId);
        likedTrack.Should().NotBeNull();

        var systemPlaylist = await DbContext.Playlists
            .FirstOrDefaultAsync(p => p.OwnerId == userId && p.IsSystem);
        systemPlaylist.Should().NotBeNull();
        systemPlaylist!.Title.Should().Be("Liked Songs");
        systemPlaylist.TrackCount.Should().Be(1);

        var playlistTrack = await DbContext.PlaylistTracks
            .FirstOrDefaultAsync(pt => pt.PlaylistId == systemPlaylist.Id && pt.TrackId == trackId);
        playlistTrack.Should().NotBeNull();
        playlistTrack!.Position.Should().Be(1);
    }

    [Fact]
    public async Task LikeTrack_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        var userId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        SetupUser(userId);

        var command = new LikeTrackCommand(trackId);
        var result1 = await BuildLikeHandler().Handle(command, CancellationToken.None);
        var result2 = await BuildLikeHandler().Handle(command, CancellationToken.None);

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var likedTracksCount = await DbContext.LikedTracks
            .CountAsync(lt => lt.UserId == userId && lt.TrackId == trackId);
        likedTracksCount.Should().Be(1);

        var systemPlaylist = await DbContext.Playlists
            .FirstOrDefaultAsync(p => p.OwnerId == userId && p.IsSystem);
        systemPlaylist!.TrackCount.Should().Be(1);
    }

    [Fact]
    public async Task LikeTrack_ShouldAppendToExistingLikedSongsPlaylist()
    {
        var userId = Guid.NewGuid();
        var track1 = Guid.NewGuid();
        var track2 = Guid.NewGuid();
        SetupUser(userId);

        // Pre-seed system playlist
        var systemPlaylist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Liked Songs",
            IsSystem = true,
            Visibility = PlaylistVisibility.Private,
            TrackCount = 1
        };
        await SeedAsync(systemPlaylist);

        var pt1 = new PlaylistTrack
        {
            PlaylistId = systemPlaylist.Id,
            TrackId = track1,
            Position = 1,
            AddedBy = userId
        };
        await SeedAsync(pt1);

        var command = new LikeTrackCommand(track2);
        var result = await BuildLikeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updatedPlaylist = await DbContext.Playlists.FirstOrDefaultAsync(p => p.Id == systemPlaylist.Id);
        updatedPlaylist!.TrackCount.Should().Be(2);

        var tracks = await DbContext.PlaylistTracks
            .Where(pt => pt.PlaylistId == systemPlaylist.Id && !pt.IsDeleted)
            .OrderBy(pt => pt.Position)
            .ToListAsync();

        tracks.Should().HaveCount(2);
        tracks[1].TrackId.Should().Be(track2);
        tracks[1].Position.Should().Be(2);
    }

    #endregion

    #region UnlikeTrack Tests

    [Fact]
    public async Task UnlikeTrack_ShouldRemoveFromLikedTracks_AndSoftDeleteFromSystemPlaylistWithRegap()
    {
        var userId = Guid.NewGuid();
        var track1 = Guid.NewGuid();
        var track2 = Guid.NewGuid();
        var track3 = Guid.NewGuid();
        SetupUser(userId);

        var systemPlaylist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Liked Songs",
            IsSystem = true,
            Visibility = PlaylistVisibility.Private,
            TrackCount = 3
        };
        await SeedAsync(systemPlaylist);

        await SeedAsync(new LikedTrack { UserId = userId, TrackId = track1 });
        await SeedAsync(new LikedTrack { UserId = userId, TrackId = track2 });
        await SeedAsync(new LikedTrack { UserId = userId, TrackId = track3 });

        await SeedAsync(new PlaylistTrack { PlaylistId = systemPlaylist.Id, TrackId = track1, Position = 1, AddedBy = userId });
        await SeedAsync(new PlaylistTrack { PlaylistId = systemPlaylist.Id, TrackId = track2, Position = 2, AddedBy = userId });
        await SeedAsync(new PlaylistTrack { PlaylistId = systemPlaylist.Id, TrackId = track3, Position = 3, AddedBy = userId });

        // Unlike track 2 (middle track)
        var command = new UnlikeTrackCommand(track2);
        var result = await BuildUnlikeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var isStillLiked = await DbContext.LikedTracks
            .AnyAsync(lt => lt.UserId == userId && lt.TrackId == track2);
        isStillLiked.Should().BeFalse();

        var activeTracks = await DbContext.PlaylistTracks
            .Where(pt => pt.PlaylistId == systemPlaylist.Id && !pt.IsDeleted)
            .OrderBy(pt => pt.Position)
            .ToListAsync();

        activeTracks.Should().HaveCount(2);
        activeTracks[0].TrackId.Should().Be(track1);
        activeTracks[0].Position.Should().Be(1);
        activeTracks[1].TrackId.Should().Be(track3);
        activeTracks[1].Position.Should().Be(2); // Was position 3, now shifted to 2

        var updatedPlaylist = await DbContext.Playlists.FirstOrDefaultAsync(p => p.Id == systemPlaylist.Id);
        updatedPlaylist!.TrackCount.Should().Be(2);
    }

    #endregion
}
