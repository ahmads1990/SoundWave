using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Albums.UploadAlbumCover;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Storage.Services;

namespace SoundWave.Catalog.Tests.Features.Albums;

public class UploadAlbumCoverTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<UploadAlbumCoverCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IImageUploadService> _imageUploadServiceMock = new();

    private UploadAlbumCoverCommandHandler BuildHandler()
    {
        return new UploadAlbumCoverCommandHandler(
            CreateRepository<Album>(),
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
    public async Task Handle_ShouldReturnAlbumNotFound_WhenAlbumDoesNotExist()
    {
        SetupUser(Guid.NewGuid());
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadAlbumCoverCommand(Guid.NewGuid(), stream, "cover.png", stream.Length);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorizedAlbumAccess_WhenUserIsNotPrimaryArtist()
    {
        var primaryArtistUserId = Guid.NewGuid();
        var nonPrimaryUserId = Guid.NewGuid();
        SetupUser(nonPrimaryUserId);

        var primaryArtist = new Artist { UserId = primaryArtistUserId, StageName = "Main Artist" };
        await SeedAsync(primaryArtist);

        var album = new Album { Title = "Test Album", AlbumType = AlbumType.Album };
        album.AlbumArtists.Add(new AlbumArtist { ArtistId = primaryArtist.Id, Order = 0 });
        await SeedAsync(album);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadAlbumCoverCommand(album.Id, stream, "cover.png", stream.Length);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedAlbumAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnFileValidationFailed_WhenImageUploadServiceFails()
    {
        var primaryArtistUserId = Guid.NewGuid();
        SetupUser(primaryArtistUserId);

        var primaryArtist = new Artist { UserId = primaryArtistUserId, StageName = "Main Artist" };
        await SeedAsync(primaryArtist);

        var album = new Album { Title = "Test Album", AlbumType = AlbumType.Album };
        album.AlbumArtists.Add(new AlbumArtist { ArtistId = primaryArtist.Id, Order = 0 });
        await SeedAsync(album);

        _imageUploadServiceMock
            .Setup(s => s.ReplaceImageAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), FileType.AlbumCover, It.IsAny<string?>(),
                It.IsAny<long>(), It.IsAny<SupportedMediaFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SharedKernelError, ImageUploadResult>.Failure(
                SharedKernelError.UnsupportedFileExtension, "Bad extension"));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadAlbumCoverCommand(album.Id, stream, "cover.xyz", stream.Length);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.FileValidationFailed);
    }

    [Fact]
    public async Task Handle_ShouldUpdateCoverImageUrl_WhenUserIsPrimaryArtistAndUploadSucceeds()
    {
        var primaryArtistUserId = Guid.NewGuid();
        SetupUser(primaryArtistUserId);

        var primaryArtist = new Artist { UserId = primaryArtistUserId, StageName = "Main Artist" };
        await SeedAsync(primaryArtist);

        var album = new Album
        {
            Title = "Masterpiece Album",
            AlbumType = AlbumType.Album,
            CoverImageUrl = "http://storage.soundwave/old-cover.webp"
        };
        album.AlbumArtists.Add(new AlbumArtist { ArtistId = primaryArtist.Id, Order = 0 });
        await SeedAsync(album);

        var newFileId = Guid.NewGuid();
        var newCoverUrl = "http://storage.soundwave/public/images/covers/albums/new.webp";

        _imageUploadServiceMock
            .Setup(s => s.ReplaceImageAsync(
                It.IsAny<Stream>(), "cover.png", FileType.AlbumCover, "http://storage.soundwave/old-cover.webp",
                It.IsAny<long>(), It.IsAny<SupportedMediaFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SharedKernelError, ImageUploadResult>.Success(
                new ImageUploadResult(newFileId, "key", newCoverUrl, "image/webp", 1024)));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadAlbumCoverCommand(album.Id, stream, "cover.png", stream.Length);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileId.Should().Be(newFileId);
        result.Data.Url.Should().Be(newCoverUrl);

        var updatedAlbum = await CreateReadDbContext().Albums.FindAsync(album.Id);
        updatedAlbum.Should().NotBeNull();
        updatedAlbum!.CoverImageUrl.Should().Be(newCoverUrl);
    }
}
