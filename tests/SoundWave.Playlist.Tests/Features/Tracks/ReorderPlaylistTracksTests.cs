using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Tracks.ReorderPlaylistTracks;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Tracks;

public class ReorderPlaylistTracksTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<ReorderPlaylistTracksCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly ReorderPlaylistTracksRequestValidator _validator = new();

    private ReorderPlaylistTracksCommandHandler BuildHandler()
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

        var command = new ReorderPlaylistTracksCommand(Guid.NewGuid(), Guid.NewGuid(), 2);
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

        var command = new ReorderPlaylistTracksCommand(systemPlaylist.Id, Guid.NewGuid(), 1);
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

        var command = new ReorderPlaylistTracksCommand(playlist.Id, Guid.NewGuid(), 2);
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

        var command = new ReorderPlaylistTracksCommand(playlist.Id, Guid.NewGuid(), 1);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.TrackNotInPlaylist);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenNewPositionIsSameAsCurrentPosition()
    {
        var userId = Guid.NewGuid();
        var track1 = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "1 Track Playlist",
            IsSystem = false,
            Visibility = PlaylistVisibility.Private,
            TrackCount = 1
        };
        await SeedAsync(playlist);

        var pt1 = new PlaylistTrack { PlaylistId = playlist.Id, TrackId = track1, Position = 1, AddedBy = userId };
        await SeedAsync(pt1);

        var command = new ReorderPlaylistTracksCommand(playlist.Id, track1, 1);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldShiftIntermediateTracksUp_WhenMovingTrackDown()
    {
        // Setup 4 tracks: T1 (pos 1), T2 (pos 2), T3 (pos 3), T4 (pos 4)
        // Move T1 to pos 3 -> Expected order: T2 (1), T3 (2), T1 (3), T4 (4)
        var userId = Guid.NewGuid();
        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        var t3 = Guid.NewGuid();
        var t4 = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Reorder Down Test",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public,
            TrackCount = 4
        };
        await SeedAsync(playlist);

        await SeedAsync(new PlaylistTrack { PlaylistId = playlist.Id, TrackId = t1, Position = 1, AddedBy = userId });
        await SeedAsync(new PlaylistTrack { PlaylistId = playlist.Id, TrackId = t2, Position = 2, AddedBy = userId });
        await SeedAsync(new PlaylistTrack { PlaylistId = playlist.Id, TrackId = t3, Position = 3, AddedBy = userId });
        await SeedAsync(new PlaylistTrack { PlaylistId = playlist.Id, TrackId = t4, Position = 4, AddedBy = userId });

        var command = new ReorderPlaylistTracksCommand(playlist.Id, t1, 3);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var reordered = await DbContext.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlist.Id)
            .OrderBy(pt => pt.Position)
            .ToListAsync();

        reordered.Should().HaveCount(4);
        reordered[0].TrackId.Should().Be(t2);
        reordered[0].Position.Should().Be(1);
        reordered[1].TrackId.Should().Be(t3);
        reordered[1].Position.Should().Be(2);
        reordered[2].TrackId.Should().Be(t1);
        reordered[2].Position.Should().Be(3);
        reordered[3].TrackId.Should().Be(t4);
        reordered[3].Position.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ShouldShiftIntermediateTracksDown_WhenMovingTrackUp()
    {
        // Setup 4 tracks: T1 (pos 1), T2 (pos 2), T3 (pos 3), T4 (pos 4)
        // Move T4 to pos 2 -> Expected order: T1 (1), T4 (2), T2 (3), T3 (4)
        var userId = Guid.NewGuid();
        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        var t3 = Guid.NewGuid();
        var t4 = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Reorder Up Test",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public,
            TrackCount = 4
        };
        await SeedAsync(playlist);

        await SeedAsync(new PlaylistTrack { PlaylistId = playlist.Id, TrackId = t1, Position = 1, AddedBy = userId });
        await SeedAsync(new PlaylistTrack { PlaylistId = playlist.Id, TrackId = t2, Position = 2, AddedBy = userId });
        await SeedAsync(new PlaylistTrack { PlaylistId = playlist.Id, TrackId = t3, Position = 3, AddedBy = userId });
        await SeedAsync(new PlaylistTrack { PlaylistId = playlist.Id, TrackId = t4, Position = 4, AddedBy = userId });

        var command = new ReorderPlaylistTracksCommand(playlist.Id, t4, 2);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var reordered = await DbContext.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlist.Id)
            .OrderBy(pt => pt.Position)
            .ToListAsync();

        reordered.Should().HaveCount(4);
        reordered[0].TrackId.Should().Be(t1);
        reordered[0].Position.Should().Be(1);
        reordered[1].TrackId.Should().Be(t4);
        reordered[1].Position.Should().Be(2);
        reordered[2].TrackId.Should().Be(t2);
        reordered[2].Position.Should().Be(3);
        reordered[3].TrackId.Should().Be(t3);
        reordered[3].Position.Should().Be(4);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenTrackIdIsEmpty()
    {
        var request = new ReorderPlaylistTracksRequest(Guid.Empty, 2);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReorderPlaylistTracksRequest.TrackId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_ShouldFail_WhenPositionIsLessThanOne(int invalidPosition)
    {
        var request = new ReorderPlaylistTracksRequest(Guid.NewGuid(), invalidPosition);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReorderPlaylistTracksRequest.NewPosition));
    }

    [Fact]
    public void Validator_ShouldPass_ForValidRequest()
    {
        var request = new ReorderPlaylistTracksRequest(Guid.NewGuid(), 3);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    #endregion
}
