using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Playlists.ListPublicPlaylists;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Playlists;

public class ListPublicPlaylistsTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<ILogger<ListPublicPlaylistsQueryHandler>> _loggerMock = new();

    private ListPublicPlaylistsQueryHandler BuildHandler()
    {
        return new(CreateReadDbContext(), _cachingServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedPublicPlaylists_ExcludingPrivateAndSystem()
    {
        var publicPlaylist = new Data.Entities.Playlist
        {
            Title = "Public Synthwave",
            Visibility = PlaylistVisibility.Public,
            IsSystem = false,
            FollowerCount = 10
        };
        var privatePlaylist = new Data.Entities.Playlist
        {
            Title = "Private Secret",
            Visibility = PlaylistVisibility.Private,
            IsSystem = false
        };
        var systemPlaylist = new Data.Entities.Playlist
        {
            Title = "Liked Songs",
            Visibility = PlaylistVisibility.Public,
            IsSystem = true
        };

        await SeedAsync(publicPlaylist, privatePlaylist, systemPlaylist);

        var query = new ListPublicPlaylistsQuery();
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Title.Should().Be("Public Synthwave");
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm()
    {
        var p1 = new Data.Entities.Playlist { Title = "Jazz Lounge", Visibility = PlaylistVisibility.Public, IsSystem = false };
        var p2 = new Data.Entities.Playlist { Title = "Rock Stadium", Visibility = PlaylistVisibility.Public, IsSystem = false };
        await SeedAsync(p1, p2);

        var query = new ListPublicPlaylistsQuery(SearchTerm: "Jazz");
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Title.Should().Be("Jazz Lounge");
    }
}
