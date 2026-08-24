using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Features.Playlists.EditPlaylist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Playlists;

public class EditPlaylistTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<EditPlaylistCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly EditPlaylistRequestValidator _validator = new();

    private EditPlaylistCommandHandler BuildHandler()
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

        var command = new EditPlaylistCommand(Guid.NewGuid(), "New Title", "New Desc", PlaylistVisibility.Public);
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

        var command = new EditPlaylistCommand(systemPlaylist.Id, "Renamed Songs", "Hacked", PlaylistVisibility.Public);
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

        var command = new EditPlaylistCommand(playlist.Id, "Hacked Title", null, PlaylistVisibility.Private);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldUpdatePlaylist_WhenCallerIsOwner()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Old Title",
            Description = "Old Description",
            IsSystem = false,
            Visibility = PlaylistVisibility.Private
        };
        await SeedAsync(playlist);

        var command = new EditPlaylistCommand(playlist.Id, "Updated Title", "Updated Description", PlaylistVisibility.Public);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        var updated = await DbContext.Playlists.FirstOrDefaultAsync(p => p.Id == playlist.Id);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Title");
        updated.Description.Should().Be("Updated Description");
        updated.Visibility.Should().Be(PlaylistVisibility.Public);
    }

    #endregion

    #region Validator Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_ShouldFail_WhenTitleIsEmpty(string? title)
    {
        var request = new EditPlaylistRequest(title!);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EditPlaylistRequest.Title));
    }

    [Fact]
    public void Validator_ShouldFail_WhenDescriptionExceeds1000Characters()
    {
        var longDesc = new string('E', 1001);
        var request = new EditPlaylistRequest("Valid Title", longDesc);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EditPlaylistRequest.Description));
    }

    #endregion
}
