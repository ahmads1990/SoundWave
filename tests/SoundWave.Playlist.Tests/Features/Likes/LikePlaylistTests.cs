using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Likes.LikePlaylist;
using SoundWave.Playlist.Features.Likes.UnlikePlaylist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Likes;

public class LikePlaylistTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<LikePlaylistCommandHandler>> _likeLoggerMock = new();
    private readonly Mock<ILogger<UnlikePlaylistCommandHandler>> _unlikeLoggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private LikePlaylistCommandHandler BuildLikeHandler()
    {
        return new(DbContext, _currentUserMock.Object, _likeLoggerMock.Object);
    }

    private UnlikePlaylistCommandHandler BuildUnlikeHandler()
    {
        return new(DbContext, _currentUserMock.Object, _unlikeLoggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    [Fact]
    public async Task LikePlaylist_ShouldReturnPlaylistNotFound_WhenPlaylistDoesNotExist()
    {
        SetupUser(Guid.NewGuid());

        var command = new LikePlaylistCommand(Guid.NewGuid());
        var result = await BuildLikeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.PlaylistNotFound);
    }

    [Fact]
    public async Task LikePlaylist_ShouldReturnPlaylistNotFound_WhenPlaylistIsPrivateAndCallerIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        SetupUser(callerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "Private Vibes",
            Visibility = PlaylistVisibility.Private
        };
        await SeedAsync(playlist);

        var command = new LikePlaylistCommand(playlist.Id);
        var result = await BuildLikeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.PlaylistNotFound);
    }

    [Fact]
    public async Task LikePlaylist_ShouldSavePlaylistToLibrary_AndIncrementFollowerCount()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        SetupUser(callerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "Public Synthwave",
            Visibility = PlaylistVisibility.Public,
            FollowerCount = 0
        };
        await SeedAsync(playlist);

        var command = new LikePlaylistCommand(playlist.Id);
        var result = await BuildLikeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var liked = await DbContext.LikedPlaylists
            .FirstOrDefaultAsync(lp => lp.UserId == callerId && lp.PlaylistId == playlist.Id);
        liked.Should().NotBeNull();

        var updatedPlaylist = await DbContext.Playlists.FirstOrDefaultAsync(p => p.Id == playlist.Id);
        updatedPlaylist!.FollowerCount.Should().Be(1);
    }

    [Fact]
    public async Task LikePlaylist_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        SetupUser(callerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "Public Synthwave",
            Visibility = PlaylistVisibility.Public,
            FollowerCount = 0
        };
        await SeedAsync(playlist);

        var command = new LikePlaylistCommand(playlist.Id);
        var r1 = await BuildLikeHandler().Handle(command, CancellationToken.None);
        var r2 = await BuildLikeHandler().Handle(command, CancellationToken.None);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();

        var count = await DbContext.LikedPlaylists
            .CountAsync(lp => lp.UserId == callerId && lp.PlaylistId == playlist.Id);
        count.Should().Be(1);

        var updatedPlaylist = await DbContext.Playlists.FirstOrDefaultAsync(p => p.Id == playlist.Id);
        updatedPlaylist!.FollowerCount.Should().Be(1);
    }

    [Fact]
    public async Task UnlikePlaylist_ShouldRemoveFromLibrary_AndDecrementFollowerCount()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        SetupUser(callerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "Public Synthwave",
            Visibility = PlaylistVisibility.Public,
            FollowerCount = 1
        };
        await SeedAsync(playlist);

        await SeedAsync(new LikedPlaylist { UserId = callerId, PlaylistId = playlist.Id });

        var command = new UnlikePlaylistCommand(playlist.Id);
        var result = await BuildUnlikeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var exists = await DbContext.LikedPlaylists
            .AnyAsync(lp => lp.UserId == callerId && lp.PlaylistId == playlist.Id);
        exists.Should().BeFalse();

        var updatedPlaylist = await DbContext.Playlists.FirstOrDefaultAsync(p => p.Id == playlist.Id);
        updatedPlaylist!.FollowerCount.Should().Be(0);
    }
}
