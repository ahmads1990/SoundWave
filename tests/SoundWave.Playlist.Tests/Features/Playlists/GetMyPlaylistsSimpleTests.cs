using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Playlists.GetMyPlaylistsSimple;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Playlists;

public class GetMyPlaylistsSimpleTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<GetMyPlaylistsSimpleQueryHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private GetMyPlaylistsSimpleQueryHandler BuildHandler()
    {
        return new(CreateReadDbContext(), _currentUserMock.Object, _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyUserOwnedNonSystemPlaylists()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupUser(userId);

        var myCustomPlaylist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Gym Workout",
            IsSystem = false,
            TrackCount = 3
        };
        var mySystemPlaylist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Liked Songs",
            IsSystem = true,
            TrackCount = 10
        };
        var otherUserPlaylist = new Data.Entities.Playlist
        {
            OwnerId = otherUserId,
            Title = "Other's Playlist",
            IsSystem = false
        };

        await SeedAsync(myCustomPlaylist, mySystemPlaylist, otherUserPlaylist);

        var query = new GetMyPlaylistsSimpleQuery();
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Id.Should().Be(myCustomPlaylist.Id);
        result.Data[0].Title.Should().Be("Gym Workout");
        result.Data[0].TrackCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldSetContainsTrackTrue_WhenTrackIsInPlaylist()
    {
        var userId = Guid.NewGuid();
        var targetTrackId = Guid.NewGuid();
        SetupUser(userId);

        var playlistWithTrack = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Pop Anthems",
            IsSystem = false
        };
        var playlistWithoutTrack = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Chill Vibes",
            IsSystem = false
        };
        await SeedAsync(playlistWithTrack, playlistWithoutTrack);

        var track = new PlaylistTrack
        {
            PlaylistId = playlistWithTrack.Id,
            TrackId = targetTrackId,
            Position = 1
        };
        await SeedAsync(track);

        var query = new GetMyPlaylistsSimpleQuery(targetTrackId);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);

        var withTrack = result.Data!.First(p => p.Id == playlistWithTrack.Id);
        withTrack.ContainsTrack.Should().BeTrue();

        var withoutTrack = result.Data.First(p => p.Id == playlistWithoutTrack.Id);
        withoutTrack.ContainsTrack.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldFilterByName_WhenSearchTermProvided()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var p1 = new Data.Entities.Playlist { OwnerId = userId, Title = "Acoustic Morning", IsSystem = false };
        var p2 = new Data.Entities.Playlist { OwnerId = userId, Title = "Evening Jazz", IsSystem = false };
        await SeedAsync(p1, p2);

        var query = new GetMyPlaylistsSimpleQuery(SearchTerm: "Acoustic");
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Id.Should().Be(p1.Id);
        result.Data[0].Title.Should().Be("Acoustic Morning");
    }
}
