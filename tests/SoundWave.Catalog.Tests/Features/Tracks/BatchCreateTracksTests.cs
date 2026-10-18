using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.Catalog.Features.Tracks.BatchCreateTracks;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Storage;
using SoundWave.SharedKernel.Validation;

namespace SoundWave.Catalog.Tests.Features.Tracks;

public class BatchCreateTracksTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<BatchCreateTracksCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<IObjectStorageService> _storageServiceMock = new();
    private readonly Mock<IFileMetadataRepository> _fileMetadataRepositoryMock = new();
    private readonly BatchCreateTracksRequestValidator _validator = new();

    private BatchCreateTracksCommandHandler BuildHandler()
    {
        var genreQueryHandler = new ValidateGenresExistQueryHandler(CreateReadDbContext());
        _senderMock.Setup(s => s.Send(It.IsAny<ValidateGenresExistQuery>(), It.IsAny<CancellationToken>()))
            .Returns<IRequest<bool>, CancellationToken>((q, ct) => genreQueryHandler.Handle((ValidateGenresExistQuery)q, ct));

        _storageServiceMock.Setup(s => s.GenerateUploadUrlAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, string key, TimeSpan expiry, string? ct, IDictionary<string, string>? meta, CancellationToken token) =>
                $"https://s3.local/soundwave-storage/{key}?signed=true");

        _fileMetadataRepositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyCollection<FileMetadata>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new(
            DbContext,
            CreateRepository<Track>(),
            CreateRepository<Album>(),
            CreateRepository<Artist>(),
            _fileMetadataRepositoryMock.Object,
            _storageServiceMock.Object,
            _senderMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    private async Task<(Artist Artist, Album Album)> SeedAlbumWithPrimaryArtistAsync(
        Guid userId,
        bool isPublished = false,
        int initialTrackCount = 0)
    {
        var artist = new Artist { UserId = userId, StageName = "Batch Artist", Bio = "Bio" };
        await SeedAsync(artist);

        var album = new Album
        {
            Title = "Batch Test Album",
            AlbumType = AlbumType.Album,
            IsPublished = isPublished,
            TrackCount = initialTrackCount
        };
        await SeedAsync(album);

        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = artist.Id, Order = 0 });

        return (artist, album);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldCreateAllTracksSequentially_WithPendingUploadAndPresignedUrls()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var (artist, album) = await SeedAlbumWithPrimaryArtistAsync(userId, initialTrackCount: 2);

        var tracks = new List<BatchTrackItem>
        {
            new("Track Alpha", "alpha.mp3", 5 * 1024 * 1024, "audio/mpeg"),
            new("Track Beta", "beta.flac", 15 * 1024 * 1024, "audio/flac"),
            new("Track Gamma", "gamma.wav", 20 * 1024 * 1024, "audio/wav")
        };

        var command = new BatchCreateTracksCommand(album.Id, tracks);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AlbumId.Should().Be(album.Id);
        result.Data.Tracks.Should().HaveCount(3);

        // Verify sequential track numbering starting from album.TrackCount + 1 (2 + 1 = 3)
        result.Data.Tracks[0].TrackNumber.Should().Be(3);
        result.Data.Tracks[0].Title.Should().Be("Track Alpha");
        result.Data.Tracks[0].UploadUrl.Should().Contain(".mp3");
        result.Data.Tracks[0].StorageKey.Should().Contain(".mp3");

        result.Data.Tracks[1].TrackNumber.Should().Be(4);
        result.Data.Tracks[1].Title.Should().Be("Track Beta");
        result.Data.Tracks[1].UploadUrl.Should().Contain(".flac");
        result.Data.Tracks[1].StorageKey.Should().Contain(".flac");

        result.Data.Tracks[2].TrackNumber.Should().Be(5);
        result.Data.Tracks[2].Title.Should().Be("Track Gamma");
        result.Data.Tracks[2].UploadUrl.Should().Contain(".wav");
        result.Data.Tracks[2].StorageKey.Should().Contain(".wav");

        // Verify database persistence of tracks and track files
        var dbTracks = await DbContext.Tracks
            .Include(t => t.TrackFile)
            .Include(t => t.TrackArtists)
            .Where(t => t.AlbumId == album.Id && t.TrackNumber >= 3)
            .OrderBy(t => t.TrackNumber)
            .ToListAsync();

        dbTracks.Should().HaveCount(3);
        foreach (var t in dbTracks)
        {
            t.TrackFile.Should().NotBeNull();
            t.TrackFile!.Status.Should().Be(FileStatus.PendingUpload);
            t.TrackFile.FileId.Should().NotBeNull();
            t.TrackArtists.Should().ContainSingle(ta => ta.ArtistId == artist.Id && ta.Order == 0);
        }

        // Verify album track count updated
        var updatedAlbum = await DbContext.Albums.AsNoTracking().FirstOrDefaultAsync(a => a.Id == album.Id);
        updatedAlbum!.TrackCount.Should().Be(5);

        // Verify FileMetadata persisted in single batch
        _fileMetadataRepositoryMock.Verify(
            r => r.AddRangeAsync(It.Is<IReadOnlyCollection<FileMetadata>>(m => m.Count == 3), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlbumNotFound_WhenAlbumDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        await SeedAsync(new Artist { UserId = userId, StageName = "Artist", Bio = "Bio" });

        var command = new BatchCreateTracksCommand(
            Guid.NewGuid(),
            [new("Track 1", "track.mp3", 1024)]);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotPrimaryArtist()
    {
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupUser(otherUserId);

        var ownerArtist = new Artist { UserId = ownerUserId, StageName = "Owner", Bio = "Bio" };
        var otherArtist = new Artist { UserId = otherUserId, StageName = "Other", Bio = "Bio" };
        await SeedAsync(ownerArtist, otherArtist);

        var album = new Album { Title = "Album", AlbumType = AlbumType.Album, IsPublished = false, TrackCount = 0 };
        await SeedAsync(album);
        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = ownerArtist.Id, Order = 0 });

        var command = new BatchCreateTracksCommand(
            album.Id,
            [new("Track 1", "track.mp3", 1024)]);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedAlbumAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlbumAlreadyPublished_WhenAlbumIsPublished()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var (_, album) = await SeedAlbumWithPrimaryArtistAsync(userId, isPublished: true);

        var command = new BatchCreateTracksCommand(
            album.Id,
            [new("Track 1", "track.mp3", 1024)]);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumAlreadyPublished);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidGenreId_WhenGenreDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var (_, album) = await SeedAlbumWithPrimaryArtistAsync(userId);

        var command = new BatchCreateTracksCommand(
            album.Id,
            [new("Track with Invalid Genre", "song.mp3", 1024, GenreIds: [9999])]);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.InvalidGenreId);
    }

    [Fact]
    public async Task Handle_ShouldReturnArtistNotFound_WhenFeaturedArtistDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var (_, album) = await SeedAlbumWithPrimaryArtistAsync(userId);

        var command = new BatchCreateTracksCommand(
            album.Id,
            [new("Track with Missing Feature", "song.mp3", 1024, FeaturedArtistIds: [Guid.NewGuid()])]);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.ArtistNotFound);
    }

    [Fact]
    public async Task Handle_ShouldAssignFeaturedArtistsAndGenres_WhenProvided()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var (primaryArtist, album) = await SeedAlbumWithPrimaryArtistAsync(userId);

        var featuredArtist = new Artist { UserId = Guid.NewGuid(), StageName = "Featured Guest", Bio = "Bio" };
        var genre = new Genre { Name = "Deep House", Type = GenreType.Genre };
        await SeedAsync(featuredArtist);
        await SeedAsync(genre);

        var command = new BatchCreateTracksCommand(
            album.Id,
            [
                new(
                    "Collaborative Banger",
                    "collab.flac",
                    10 * 1024 * 1024,
                    "audio/flac",
                    GenreIds: [genre.Id],
                    FeaturedArtistIds: [featuredArtist.Id])
            ]);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var track = await DbContext.Tracks
            .Include(t => t.TrackArtists)
            .Include(t => t.TrackGenres)
            .FirstOrDefaultAsync(t => t.Id == result.Data!.Tracks[0].TrackId);

        track.Should().NotBeNull();
        track!.TrackArtists.Should().HaveCount(2);
        track.TrackArtists.Should().Contain(ta => ta.ArtistId == primaryArtist.Id && ta.Order == 0);
        track.TrackArtists.Should().Contain(ta => ta.ArtistId == featuredArtist.Id && ta.Order == 1);
        track.TrackGenres.Should().ContainSingle(tg => tg.GenreId == genre.Id);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenTracksListIsEmpty()
    {
        var request = new BatchCreateTracksRequest([]);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tracks");
    }

    [Fact]
    public void Validator_ShouldFail_WhenTrackTitleIsEmpty()
    {
        var request = new BatchCreateTracksRequest([
            new BatchTrackItemRequest("", "audio.mp3", 1024)
        ]);

        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Title"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenAudioExtensionIsInvalid()
    {
        var request = new BatchCreateTracksRequest([
            new BatchTrackItemRequest("Track", "executable.exe", 1024)
        ]);

        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("audio formats"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenFileSizeBytesExceedsMaxAudioSize()
    {
        var request = new BatchCreateTracksRequest([
            new BatchTrackItemRequest("Large Track", "track.mp3", FileMetadataValidator.DefaultMaxAudioSizeBytes + 1)
        ]);

        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("exceeds the maximum allowed size"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenBatchExceedsMaxBatchSize()
    {
        var items = Enumerable.Range(1, BatchCreateTracksRequestValidator.MaxBatchSize + 1)
            .Select(i => new BatchTrackItemRequest($"Track {i}", $"track_{i}.mp3", 1024))
            .ToList();

        var request = new BatchCreateTracksRequest(items);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tracks");
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenBatchIsValid()
    {
        var request = new BatchCreateTracksRequest([
            new BatchTrackItemRequest("Track 1", "audio1.mp3", 1024),
            new BatchTrackItemRequest("Track 2", "audio2.flac", 2048)
        ]);

        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
