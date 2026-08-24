using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Features.Playlists.DeletePlaylist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Playlists;

public class DeletePlaylistTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<DeletePlaylistCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private DeletePlaylistCommandHandler BuildHandler()
    {
        return new(
            CreateRepository<Data.Entities.Playlist>(),
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

        var command = new DeletePlaylistCommand(Guid.NewGuid());
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

        var command = new DeletePlaylistCommand(systemPlaylist.Id);
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

        var command = new DeletePlaylistCommand(playlist.Id);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldSoftDeletePlaylist_WhenCallerIsOwner()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Temporary Playlist",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public
        };
        await SeedAsync(playlist);

        var command = new DeletePlaylistCommand(playlist.Id);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        var deleted = await DbContext.Playlists.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == playlist.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    #endregion
}
