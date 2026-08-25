using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Playlists.GetUserPublicPlaylists;

namespace SoundWave.Playlist.Tests.Features.Playlists;

public class GetUserPublicPlaylistsTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<GetUserPublicPlaylistsQueryHandler>> _loggerMock = new();

    private GetUserPublicPlaylistsQueryHandler BuildHandler()
    {
        return new(CreateReadDbContext(), _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPublicPlaylistsCreatedBySpecificUser()
    {
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var userPublicPlaylist = new Data.Entities.Playlist
        {
            OwnerId = targetUserId,
            Title = "Artist Picks",
            Visibility = PlaylistVisibility.Public,
            IsSystem = false,
            FollowerCount = 100
        };
        var userPrivatePlaylist = new Data.Entities.Playlist
        {
            OwnerId = targetUserId,
            Title = "Artist Unreleased Drafts",
            Visibility = PlaylistVisibility.Private,
            IsSystem = false
        };
        var otherUserPublicPlaylist = new Data.Entities.Playlist
        {
            OwnerId = otherUserId,
            Title = "Other Public",
            Visibility = PlaylistVisibility.Public,
            IsSystem = false
        };

        await SeedAsync(userPublicPlaylist, userPrivatePlaylist, otherUserPublicPlaylist);

        var query = new GetUserPublicPlaylistsQuery(targetUserId);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Id.Should().Be(userPublicPlaylist.Id);
        result.Data[0].Title.Should().Be("Artist Picks");
        result.Data[0].FollowerCount.Should().Be(100);
    }
}
