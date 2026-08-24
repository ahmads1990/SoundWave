using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Tracks.RemoveTrackFromPlaylist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Tracks;

public class RemoveTrackFromPlaylistTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<RemoveTrackFromPlaylistCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private RemoveTrackFromPlaylistCommandHandler BuildHandler()
    {
        return new(
            CreateRepository<Data.Entities.Playlist>(),
            CreateRepository<PlaylistTrack>(),
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnPlaylistNotFound_WhenPlaylistDoesNotExist()
    {
        SetupUser(Guid.NewGuid());

        var command = new RemoveTrackFromPlaylistCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.PlaylistNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnSystemPlaylistProtected_WhenIsSystemIsTrue()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var systemPlaylist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Liked Songs",
            IsSystem = true,
            Visibility = PlaylistVisibility.Private
        };
        await SeedAsync(systemPlaylist);

        var command = new RemoveTrackFromPlaylistCommand(systemPlaylist.Id, Guid.NewGuid());
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.SystemPlaylistProtected);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenCallerIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        SetupUser(callerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "Owner Playlist",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public
        };
        await SeedAsync(playlist);

        var command = new RemoveTrackFromPlaylistCommand(playlist.Id, Guid.NewGuid());
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrackNotInPlaylist_WhenTrackNotFoundInPlaylist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "My Playlist",
            IsSystem = false,
            Visibility = PlaylistVisibility.Private
        };
        await SeedAsync(playlist);

        var command = new RemoveTrackFromPlaylistCommand(playlist.Id, Guid.NewGuid());
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.TrackNotInPlaylist);
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteTrack_ReGapRemainingPositions_AndDecrementTrackCount()
    {
        var userId = Guid.NewGuid();
        var track1 = Guid.NewGuid();
        var track2 = Guid.NewGuid();
        var track3 = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "3 Track Playlist",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public,
            TrackCount = 3
        };
        await SeedAsync(playlist);

        var pt1 = new PlaylistTrack { PlaylistId = playlist.Id, TrackId = track1, Position = 1, AddedBy = userId };
        var pt2 = new PlaylistTrack { PlaylistId = playlist.Id, TrackId = track2, Position = 2, AddedBy = userId };
        var pt3 = new PlaylistTrack { PlaylistId = playlist.Id, TrackId = track3, Position = 3, AddedBy = userId };
        await SeedAsync(pt1);
        await SeedAsync(pt2);
        await SeedAsync(pt3);

        // Remove track 2 (middle track)
        var command = new RemoveTrackFromPlaylistCommand(playlist.Id, track2);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Check track2 is soft-deleted
        var deletedTrack = await DbContext.PlaylistTracks.IgnoreQueryFilters().FirstOrDefaultAsync(pt => pt.Id == pt2.Id);
        deletedTrack!.IsDeleted.Should().BeTrue();

        // Check active tracks and their re-gapped positions
        var activeTracks = await DbContext.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlist.Id && !pt.IsDeleted)
            .OrderBy(pt => pt.Position)
            .ToListAsync();

        activeTracks.Should().HaveCount(2);
        activeTracks[0].TrackId.Should().Be(track1);
        activeTracks[0].Position.Should().Be(1);
        activeTracks[1].TrackId.Should().Be(track3);
        activeTracks[1].Position.Should().Be(2); // Was position 3, now shifted up to 2

        var updatedPlaylist = await DbContext.Playlists.FirstOrDefaultAsync(p => p.Id == playlist.Id);
        updatedPlaylist!.TrackCount.Should().Be(2);
    }

    #endregion
}
