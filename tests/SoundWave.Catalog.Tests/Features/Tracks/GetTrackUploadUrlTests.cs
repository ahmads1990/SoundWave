using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Tracks.GetTrackUploadUrl;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Storage;

namespace SoundWave.Catalog.Tests.Features.Tracks;

public class GetTrackUploadUrlTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<GetTrackUploadUrlCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IObjectStorageService> _storageServiceMock = new();
    private readonly Mock<IFileMetadataRepository> _fileMetadataRepositoryMock = new();
    private readonly GetTrackUploadUrlRequestValidator _validator = new();

    private GetTrackUploadUrlCommandHandler BuildHandler()
    {
        return new GetTrackUploadUrlCommandHandler(
            CreateRepository<Track>(),
            _fileMetadataRepositoryMock.Object,
            _storageServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    private async Task<(Artist Artist, Album Album, Track Track)> SeedTrackWithPrimaryArtistAsync(
        Guid userId,
        bool isPublished = false,
        Guid? previousFileId = null)
    {
        var artist = new Artist { UserId = userId, StageName = "Audio Producer", Bio = "Bio" };
        await SeedAsync(artist);

        var album = new Album
        {
            Title = "Album Audio",
            AlbumType = AlbumType.Album,
            IsPublished = isPublished,
            TrackCount = 1
        };
        await SeedAsync(album);

        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = artist.Id, Order = 0 });

        var track = new Track
        {
            AlbumId = album.Id,
            Title = "Master Track",
            TrackNumber = 1,
            TrackFile = previousFileId is null
                ? null
                : new TrackFile { Status = FileStatus.PendingUpload, FileId = previousFileId, RawFilePath = "audio/raw/old/never-uploaded.mp3" }
        };
        await SeedAsync(track);

        return (artist, album, track);
    }

    #region Validator Tests

    [Theory]
    [InlineData("song.mp3", 1024)]
    [InlineData("audio.flac", 20 * 1024 * 1024)]
    [InlineData("take.wav", 40 * 1024 * 1024)]
    [InlineData("track.aac", 5 * 1024 * 1024)]
    [InlineData("master.m4a", 15 * 1024 * 1024)]
    public void Validator_ShouldPass_ForValidAudioExtensionsAndSizes(string fileName, long sizeBytes)
    {
        var request = new GetTrackUploadUrlRequest(fileName, sizeBytes);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("document.pdf", 1024)]
    [InlineData("malware.exe", 2048)]
    [InlineData("picture.png", 5000)]
    [InlineData("noext", 1024)]
    public void Validator_ShouldFail_ForInvalidExtensions(string fileName, long sizeBytes)
    {
        var request = new GetTrackUploadUrlRequest(fileName, sizeBytes);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetTrackUploadUrlRequest.FileName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(51 * 1024 * 1024)] // > 50 MB
    public void Validator_ShouldFail_ForInvalidFileSizes(long sizeBytes)
    {
        var request = new GetTrackUploadUrlRequest("track.mp3", sizeBytes);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetTrackUploadUrlRequest.FileSizeBytes));
    }

    #endregion

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnTrackNotFound_WhenTrackDoesNotExist()
    {
        SetupUser(Guid.NewGuid());
        var command = new GetTrackUploadUrlCommand(Guid.NewGuid(), "song.mp3", 1024 * 1024);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.TrackNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorizedTrackAccess_WhenUserIsNotPrimaryArtist()
    {
        var primaryArtistUserId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        SetupUser(unauthorizedUserId);

        var (_, _, track) = await SeedTrackWithPrimaryArtistAsync(primaryArtistUserId);
        var command = new GetTrackUploadUrlCommand(track.Id, "song.mp3", 1024 * 1024);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedTrackAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlbumAlreadyPublished_WhenAlbumIsPublished()
    {
        var artistUserId = Guid.NewGuid();
        SetupUser(artistUserId);

        var (_, _, track) = await SeedTrackWithPrimaryArtistAsync(artistUserId, isPublished: true);
        var command = new GetTrackUploadUrlCommand(track.Id, "song.mp3", 1024 * 1024);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumAlreadyPublished);
    }

    [Fact]
    public async Task Handle_ShouldCreateNewFileRowAndPointTrackFileAtIt_WhenFirstUpload()
    {
        var artistUserId = Guid.NewGuid();
        SetupUser(artistUserId);

        var (_, _, track) = await SeedTrackWithPrimaryArtistAsync(artistUserId);
        var expectedUrl = "https://s3.soundwave.io/presigned?signature=abc";

        FileMetadata? createdFile = null;
        _fileMetadataRepositoryMock
            .Setup(m => m.AddAsync(It.IsAny<FileMetadata>(), It.IsAny<CancellationToken>()))
            .Callback<FileMetadata, CancellationToken>((f, _) => createdFile = f)
            .ReturnsAsync((FileMetadata f, CancellationToken _) => f);

        _storageServiceMock.Setup(s => s.GenerateUploadUrlAsync(
                It.Is<Guid>(id => id != track.Id),
                It.Is<string>(k => k.StartsWith($"audio/raw/{track.Id:N}/") && k.EndsWith(".flac")),
                It.IsAny<TimeSpan>(),
                "audio/flac",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var command = new GetTrackUploadUrlCommand(track.Id, "master.flac", 25 * 1024 * 1024, "audio/flac");

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TrackId.Should().Be(track.Id);
        result.Data.UploadUrl.Should().Be(expectedUrl);

        createdFile.Should().NotBeNull();
        createdFile!.Id.Should().NotBe(track.Id, "each upload attempt gets its own file id");
        createdFile.Status.Should().Be(FileStatus.PendingUpload);
        createdFile.StorageKey.Should().Be(StorageKeyGenerator.RawAudio(track.Id, createdFile.Id, ".flac"));
        result.Data.StorageKey.Should().Be(createdFile.StorageKey);

        var updatedTrack = await DbContext.Tracks
            .Include(t => t.TrackFile)
            .FirstOrDefaultAsync(t => t.Id == track.Id);

        updatedTrack!.TrackFile.Should().NotBeNull();
        updatedTrack.TrackFile!.FileId.Should().Be(createdFile.Id);
        updatedTrack.TrackFile.RawFilePath.Should().Be(createdFile.StorageKey);
        updatedTrack.TrackFile.Status.Should().Be(FileStatus.PendingUpload);
        updatedTrack.TrackFile.FileSizeBytes.Should().Be(25 * 1024 * 1024);

        _fileMetadataRepositoryMock.Verify(m => m.UpdateAsync(It.IsAny<FileMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateSecondRowAndSupersedePrevious_WhenReuploading()
    {
        var artistUserId = Guid.NewGuid();
        SetupUser(artistUserId);

        var (_, _, track) = await SeedTrackWithPrimaryArtistAsync(artistUserId);

        var createdFiles = new List<FileMetadata>();
        _fileMetadataRepositoryMock
            .Setup(m => m.AddAsync(It.IsAny<FileMetadata>(), It.IsAny<CancellationToken>()))
            .Callback<FileMetadata, CancellationToken>((f, _) => createdFiles.Add(f))
            .ReturnsAsync((FileMetadata f, CancellationToken _) => f);
        _fileMetadataRepositoryMock
            .Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => createdFiles.FirstOrDefault(f => f.Id == id));

        var first = await BuildHandler().Handle(
            new GetTrackUploadUrlCommand(track.Id, "take1.mp3", 1024 * 1024, "audio/mpeg"), CancellationToken.None);
        var second = await BuildHandler().Handle(
            new GetTrackUploadUrlCommand(track.Id, "take2.flac", 2 * 1024 * 1024, "audio/flac"), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();

        createdFiles.Should().HaveCount(2);
        var (oldFile, newFile) = (createdFiles[0], createdFiles[1]);
        newFile.Id.Should().NotBe(oldFile.Id);
        newFile.StorageKey.Should().NotBe(oldFile.StorageKey);

        oldFile.IsDeleted.Should().BeTrue();
        _fileMetadataRepositoryMock.Verify(m => m.UpdateAsync(
            It.Is<FileMetadata>(f => f.Id == oldFile.Id && f.IsDeleted), It.IsAny<CancellationToken>()), Times.Once);
        _storageServiceMock.Verify(s => s.DeleteAsync(oldFile.StorageKey, It.IsAny<CancellationToken>()), Times.Once);

        var updatedTrack = await DbContext.Tracks
            .Include(t => t.TrackFile)
            .FirstAsync(t => t.Id == track.Id);

        updatedTrack.TrackFile!.FileId.Should().Be(newFile.Id);
        updatedTrack.TrackFile.RawFilePath.Should().Be(newFile.StorageKey);
        updatedTrack.TrackFile.Status.Should().Be(FileStatus.PendingUpload);
    }

    [Fact]
    public async Task Handle_ShouldStillSucceed_WhenTaggingPreviousObjectFails()
    {
        var artistUserId = Guid.NewGuid();
        SetupUser(artistUserId);

        var previousFile = FileMetadata.CreatePendingUpload(
            Guid.NewGuid(), "audio/raw/old/never-uploaded.mp3", "old.mp3", FileType.Track, "audio/mpeg", 1024);

        var (_, _, track) = await SeedTrackWithPrimaryArtistAsync(artistUserId, previousFileId: previousFile.Id);

        _fileMetadataRepositoryMock
            .Setup(m => m.GetByIdAsync(previousFile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousFile);
        _storageServiceMock
            .Setup(s => s.DeleteAsync(previousFile.StorageKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("NoSuchKey"));

        var result = await BuildHandler().Handle(
            new GetTrackUploadUrlCommand(track.Id, "new.mp3", 1024 * 1024, "audio/mpeg"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        previousFile.IsDeleted.Should().BeTrue();
    }

    #endregion
}
