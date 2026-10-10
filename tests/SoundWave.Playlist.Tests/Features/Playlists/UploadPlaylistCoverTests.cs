using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Features.Playlists.UploadPlaylistCover;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Storage.Services;

namespace SoundWave.Playlist.Tests.Features.Playlists;

public class UploadPlaylistCoverTests : PlaylistIntegrationTestBase
{
    private readonly Mock<ILogger<UploadPlaylistCoverCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IImageUploadService> _imageUploadServiceMock = new();

    private UploadPlaylistCoverCommandHandler BuildHandler()
    {
        return new UploadPlaylistCoverCommandHandler(
            CreateRepository<Data.Entities.Playlist>(),
            _imageUploadServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    [Fact]
    public async Task Handle_ShouldReturnPlaylistNotFound_WhenPlaylistDoesNotExist()
    {
        SetupUser(Guid.NewGuid());
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadPlaylistCoverCommand(Guid.NewGuid(), stream, "cover.png", stream.Length);

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

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadPlaylistCoverCommand(systemPlaylist.Id, stream, "cover.png", stream.Length);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.SystemPlaylistProtected);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        SetupUser(callerId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = ownerId,
            Title = "My Jams",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public
        };
        await SeedAsync(playlist);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadPlaylistCoverCommand(playlist.Id, stream, "cover.png", stream.Length);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnFileValidationFailed_WhenImageUploadServiceFails()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Workout Vibes",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public
        };
        await SeedAsync(playlist);

        _imageUploadServiceMock
            .Setup(s => s.ReplaceImageAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), FileType.PlaylistCover, It.IsAny<string?>(),
                It.IsAny<long>(), It.IsAny<SupportedMediaFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SharedKernelError, ImageUploadResult>.Failure(
                SharedKernelError.FileExceedsMaxSize, "File too large"));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadPlaylistCoverCommand(playlist.Id, stream, "huge.png", stream.Length);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(PlaylistError.FileValidationFailed);
    }

    [Fact]
    public async Task Handle_ShouldUpdateCoverImageUrl_WhenUserIsOwnerAndUploadSucceeds()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = "Study Chill",
            IsSystem = false,
            Visibility = PlaylistVisibility.Public,
            CoverImageUrl = "http://storage.soundwave/old-playlist-cover.webp"
        };
        await SeedAsync(playlist);

        var newFileId = Guid.NewGuid();
        var newCoverUrl = "http://storage.soundwave/public/images/covers/playlists/new.webp";

        _imageUploadServiceMock
            .Setup(s => s.ReplaceImageAsync(
                It.IsAny<Stream>(), "cover.webp", FileType.PlaylistCover, "http://storage.soundwave/old-playlist-cover.webp",
                It.IsAny<long>(), It.IsAny<SupportedMediaFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SharedKernelError, ImageUploadResult>.Success(
                new ImageUploadResult(newFileId, "key", newCoverUrl, "image/webp", 2048)));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadPlaylistCoverCommand(playlist.Id, stream, "cover.webp", stream.Length);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileId.Should().Be(newFileId);
        result.Data.Url.Should().Be(newCoverUrl);

        var updatedPlaylist = await CreateReadDbContext().Playlists.FindAsync(playlist.Id);
        updatedPlaylist.Should().NotBeNull();
        updatedPlaylist!.CoverImageUrl.Should().Be(newCoverUrl);
    }
}
