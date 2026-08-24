using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Likes.LikeAlbum;
using SoundWave.Playlist.Features.Likes.UnlikeAlbum;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Likes;

public class LikeAlbumTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<LikeAlbumCommandHandler>> _likeLoggerMock = new();
    private readonly Mock<ILogger<UnlikeAlbumCommandHandler>> _unlikeLoggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private LikeAlbumCommandHandler BuildLikeHandler()
    {
        return new(DbContext, _currentUserMock.Object, _likeLoggerMock.Object);
    }

    private UnlikeAlbumCommandHandler BuildUnlikeHandler()
    {
        return new(DbContext, _currentUserMock.Object, _unlikeLoggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    [Fact]
    public async Task LikeAlbum_ShouldSaveAlbumToLibrary()
    {
        var userId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        SetupUser(userId);

        var command = new LikeAlbumCommand(albumId);
        var result = await BuildLikeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var likedAlbum = await DbContext.LikedAlbums
            .FirstOrDefaultAsync(la => la.UserId == userId && la.AlbumId == albumId);
        likedAlbum.Should().NotBeNull();
    }

    [Fact]
    public async Task LikeAlbum_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        var userId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        SetupUser(userId);

        var command = new LikeAlbumCommand(albumId);
        var r1 = await BuildLikeHandler().Handle(command, CancellationToken.None);
        var r2 = await BuildLikeHandler().Handle(command, CancellationToken.None);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();

        var count = await DbContext.LikedAlbums
            .CountAsync(la => la.UserId == userId && la.AlbumId == albumId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task UnlikeAlbum_ShouldRemoveAlbumFromLibrary()
    {
        var userId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        SetupUser(userId);

        await SeedAsync(new LikedAlbum { UserId = userId, AlbumId = albumId });

        var command = new UnlikeAlbumCommand(albumId);
        var result = await BuildUnlikeHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var exists = await DbContext.LikedAlbums
            .AnyAsync(la => la.UserId == userId && la.AlbumId == albumId);
        exists.Should().BeFalse();
    }
}
