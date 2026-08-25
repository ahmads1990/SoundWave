using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Playlists.GetLikedSongsPlaylist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Playlists;

public class GetLikedSongsPlaylistTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<GetLikedSongsPlaylistQueryHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private GetLikedSongsPlaylistQueryHandler BuildHandler()
    {
        return new(CreateReadDbContext(), _currentUserMock.Object, _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPlaylist_WhenUserHasNoSystemPlaylist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var query = new GetLikedSongsPlaylistQuery();
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be(Constants.LikedSongsPlaylistTitle);
        result.Data.IsSystem.Should().BeTrue();
        result.Data.TrackCount.Should().Be(0);
        result.Data.Tracks.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnLikedSongsPlaylistWithTracks_WhenUserHasSystemPlaylist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var systemPlaylist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = Constants.LikedSongsPlaylistTitle,
            Visibility = PlaylistVisibility.Private,
            IsSystem = true,
            TrackCount = 2
        };
        await SeedAsync(systemPlaylist);

        var track1 = new PlaylistTrack { PlaylistId = systemPlaylist.Id, TrackId = Guid.NewGuid(), Position = 1 };
        var track2 = new PlaylistTrack { PlaylistId = systemPlaylist.Id, TrackId = Guid.NewGuid(), Position = 2 };
        await SeedAsync(track1, track2);

        var query = new GetLikedSongsPlaylistQuery();
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(systemPlaylist.Id);
        result.Data.Tracks.Should().HaveCount(2);
        result.Data.Tracks[0].TrackId.Should().Be(track1.TrackId);
        result.Data.Tracks[1].TrackId.Should().Be(track2.TrackId);
    }
}
