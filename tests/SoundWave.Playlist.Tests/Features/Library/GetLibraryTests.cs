using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Library.GetLibrary;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Library;

public class GetLibraryTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<GetLibraryQueryHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private GetLibraryQueryHandler BuildHandler()
    {
        return new(CreateReadDbContext(), _currentUserMock.Object, _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    [Fact]
    public async Task Handle_ShouldAggregateOwnedPlaylists_LikedPlaylists_AndLikedAlbums()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupUser(userId);

        var ownedPlaylist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "My Own Playlist",
            IsSystem = false,
            TrackCount = 5
        };
        var followedPlaylist = new Data.Entities.Playlist
        {
            OwnerId = otherUserId,
            Title = "Followed Rock",
            Visibility = PlaylistVisibility.Public,
            TrackCount = 12
        };
        await SeedAsync(ownedPlaylist, followedPlaylist);

        var likedPlaylist = new LikedPlaylist { UserId = userId, PlaylistId = followedPlaylist.Id, LikedAt = DateTime.UtcNow };
        var likedAlbum = new LikedAlbum { UserId = userId, AlbumId = Guid.NewGuid(), LikedAt = DateTime.UtcNow.AddMinutes(-5) };
        await SeedAsync(likedPlaylist);
        await SeedAsync(likedAlbum);

        var query = new GetLibraryQuery(LibraryItemTypeFilter.All, LibrarySortBy.RecentlyAdded);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data!.Should().Contain(i => i.Id == ownedPlaylist.Id && i.ItemType == "Playlist");
        result.Data.Should().Contain(i => i.Id == followedPlaylist.Id && i.ItemType == "Playlist");
        result.Data.Should().Contain(i => i.Id == likedAlbum.AlbumId && i.ItemType == "Album");
    }

    [Fact]
    public async Task Handle_ShouldFilterByPlaylists_WhenTypeIsPlaylists()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var ownedPlaylist = new Data.Entities.Playlist { OwnerId = userId, Title = "EDM Bangers" };
        await SeedAsync(ownedPlaylist);
        await SeedAsync(new LikedAlbum { UserId = userId, AlbumId = Guid.NewGuid() });

        var query = new GetLibraryQuery(LibraryItemTypeFilter.Playlists);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Id.Should().Be(ownedPlaylist.Id);
    }

    [Fact]
    public async Task Handle_ShouldFilterByAlbums_WhenTypeIsAlbums()
    {
        var userId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        SetupUser(userId);

        var ownedPlaylist = new Data.Entities.Playlist { OwnerId = userId, Title = "EDM Bangers" };
        await SeedAsync(ownedPlaylist);
        await SeedAsync(new LikedAlbum { UserId = userId, AlbumId = albumId });

        var query = new GetLibraryQuery(LibraryItemTypeFilter.Albums);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Id.Should().Be(albumId);
        result.Data[0].ItemType.Should().Be("Album");
    }

    [Fact]
    public async Task Handle_ShouldSortAlphabetically_WhenSortByIsAlphabetical()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var p1 = new Data.Entities.Playlist { OwnerId = userId, Title = "Zebra Sounds" };
        var p2 = new Data.Entities.Playlist { OwnerId = userId, Title = "Alpha Waves" };
        await SeedAsync(p1, p2);

        var query = new GetLibraryQuery(LibraryItemTypeFilter.Playlists, LibrarySortBy.Alphabetical);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Title.Should().Be("Alpha Waves");
        result.Data[1].Title.Should().Be("Zebra Sounds");
    }
}
