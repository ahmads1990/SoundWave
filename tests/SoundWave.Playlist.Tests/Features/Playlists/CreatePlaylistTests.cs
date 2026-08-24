using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Features.Playlists.CreatePlaylist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Playlist.Tests.Features.Playlists;

public class CreatePlaylistTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<CreatePlaylistCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly CreatePlaylistRequestValidator _validator = new();

    private CreatePlaylistCommandHandler BuildHandler()
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
    public async Task Handle_ShouldCreatePlaylist_WithDefaultValues()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var command = new CreatePlaylistCommand("Night Drives", "Synthwave collection", PlaylistVisibility.Private);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);

        var saved = await DbContext.Playlists.FirstOrDefaultAsync(p => p.Id == result.Data);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Night Drives");
        saved.Description.Should().Be("Synthwave collection");
        saved.OwnerId.Should().Be(userId);
        saved.Visibility.Should().Be(PlaylistVisibility.Private);
        saved.IsSystem.Should().BeFalse();
        saved.TrackCount.Should().Be(0);
        saved.TotalDurationSeconds.Should().Be(0);
        saved.FollowerCount.Should().Be(0);
        saved.IsDeleted.Should().BeFalse();
    }

    #endregion

    #region Validator Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_ShouldFail_WhenTitleIsEmpty(string? title)
    {
        var request = new CreatePlaylistRequest(title!);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePlaylistRequest.Title));
    }

    [Fact]
    public void Validator_ShouldFail_WhenTitleExceeds100Characters()
    {
        var longTitle = new string('A', 101);
        var request = new CreatePlaylistRequest(longTitle);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePlaylistRequest.Title));
    }

    [Fact]
    public void Validator_ShouldFail_WhenDescriptionExceeds1000Characters()
    {
        var longDesc = new string('D', 1001);
        var request = new CreatePlaylistRequest("Valid Title", longDesc);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePlaylistRequest.Description));
    }

    [Fact]
    public void Validator_ShouldPass_ForValidRequest()
    {
        var request = new CreatePlaylistRequest("Workout Mix", "High energy tracks", PlaylistVisibility.Public);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    #endregion
}
