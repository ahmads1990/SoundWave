using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Tracks.AddTrackToPlaylist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Tracks;

public class AddTrackToPlaylistTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<AddTrackToPlaylistCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly AddTrackToPlaylistRequestValidator _validator = new();

    private AddTrackToPlaylistCommandHandler BuildHandler()
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

        var command = new AddTrackToPlaylistCommand(Guid.NewGuid(), Guid.NewGuid());
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

        var command = new AddTrackToPlaylistCommand(systemPlaylist.Id, Guid.NewGuid());
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
            Title = "Another User's Playlist",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public
        };
        await SeedAsync(playlist);

        var command = new AddTrackToPlaylistCommand(playlist.Id, Guid.NewGuid());
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrackAlreadyInPlaylist_WhenTrackIsAlreadyAdded()
    {
        var userId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "My Playlist",
            IsSystem = false,
            Visibility = PlaylistVisibility.Private,
            TrackCount = 1
        };
        await SeedAsync(playlist);

        var existingTrack = new PlaylistTrack
        {
            PlaylistId = playlist.Id,
            TrackId = trackId,
            Position = 1,
            AddedBy = userId
        };
        await SeedAsync(existingTrack);

        var command = new AddTrackToPlaylistCommand(playlist.Id, trackId);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.TrackAlreadyInPlaylist);
    }

    [Fact]
    public async Task Handle_ShouldAppendTrackAtNextPosition_AndIncrementTrackCount()
    {
        var userId = Guid.NewGuid();
        var track1 = Guid.NewGuid();
        var track2 = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "My Summer Hits",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public,
            TrackCount = 1
        };
        await SeedAsync(playlist);

        var existingTrack = new PlaylistTrack
        {
            PlaylistId = playlist.Id,
            TrackId = track1,
            Position = 1,
            AddedBy = userId
        };
        await SeedAsync(existingTrack);

        var command = new AddTrackToPlaylistCommand(playlist.Id, track2);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);

        var tracks = await DbContext.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlist.Id)
            .OrderBy(pt => pt.Position)
            .ToListAsync();

        tracks.Should().HaveCount(2);
        tracks[0].TrackId.Should().Be(track1);
        tracks[0].Position.Should().Be(1);
        tracks[1].TrackId.Should().Be(track2);
        tracks[1].Position.Should().Be(2);

        var updatedPlaylist = await DbContext.Playlists.FirstOrDefaultAsync(p => p.Id == playlist.Id);
        updatedPlaylist!.TrackCount.Should().Be(2);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenTrackIdIsEmpty()
    {
        var request = new AddTrackToPlaylistRequest(Guid.Empty);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddTrackToPlaylistRequest.TrackId));
    }

    [Fact]
    public void Validator_ShouldPass_ForValidRequest()
    {
        var request = new AddTrackToPlaylistRequest(Guid.NewGuid());
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    #endregion
}
