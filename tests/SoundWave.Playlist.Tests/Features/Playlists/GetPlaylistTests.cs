using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data.Entities;
using SoundWave.Playlist.Features.Playlists.GetPlaylist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Playlists;

public class GetPlaylistTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<GetPlaylistQueryHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private GetPlaylistQueryHandler BuildHandler()
    {
        return new(CreateReadDbContext(), _currentUserMock.Object, _loggerMock.Object);
    }

    private void SetupUser(Guid? userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(userId.HasValue);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    [Fact]
    public async Task Handle_ShouldReturnPlaylistNotFound_WhenPlaylistDoesNotExist()
    {
        SetupUser(Guid.NewGuid());

        var query = new GetPlaylistQuery(Guid.NewGuid());
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.PlaylistNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnPlaylistNotFound_WhenPlaylistIsPrivateAndCallerIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        SetupUser(callerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "Secret Vibes",
            Visibility = PlaylistVisibility.Private
        };
        await SeedAsync(playlist);

        var query = new GetPlaylistQuery(playlist.Id);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.PlaylistNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnPlaylist_WhenPlaylistIsPublic()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        SetupUser(callerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "Lo-Fi Beats",
            Description = "Relaxing music",
            Visibility = PlaylistVisibility.Public,
            TrackCount = 1,
            FollowerCount = 5
        };
        await SeedAsync(playlist);

        var track = new PlaylistTrack
        {
            PlaylistId = playlist.Id,
            TrackId = Guid.NewGuid(),
            Position = 1
        };
        await SeedAsync(track);

        var query = new GetPlaylistQuery(playlist.Id);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(playlist.Id);
        result.Data.Title.Should().Be("Lo-Fi Beats");
        result.Data.Description.Should().Be("Relaxing music");
        result.Data.IsOwner.Should().BeFalse();
        result.Data.IsLikedByCurrentUser.Should().BeFalse();
        result.Data.Tracks.Should().HaveCount(1);
        result.Data.Tracks[0].TrackId.Should().Be(track.TrackId);
        result.Data.Tracks[0].Position.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnPlaylist_WhenPlaylistIsPrivateAndCallerIsOwner()
    {
        var ownerId = Guid.NewGuid();
        SetupUser(ownerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "My Private Vault",
            Visibility = PlaylistVisibility.Private
        };
        await SeedAsync(playlist);

        var query = new GetPlaylistQuery(playlist.Id);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsOwner.Should().BeTrue();
        result.Data.Title.Should().Be("My Private Vault");
    }

    [Fact]
    public async Task Handle_ShouldIndicateIsLiked_WhenCallerLikedThePlaylist()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        SetupUser(callerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "Top Hits",
            Visibility = PlaylistVisibility.Public
        };
        await SeedAsync(playlist);

        await SeedAsync(new LikedPlaylist { UserId = callerId, PlaylistId = playlist.Id });

        var query = new GetPlaylistQuery(playlist.Id);
        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsLikedByCurrentUser.Should().BeTrue();
    }
}
