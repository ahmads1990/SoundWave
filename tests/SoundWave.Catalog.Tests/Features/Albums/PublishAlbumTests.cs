using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Albums.PublishAlbum;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Albums;

public class PublishAlbumTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<PublishAlbumCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private PublishAlbumCommandHandler BuildHandler()
        => new(
            CreateRepository<Album>(),
            _currentUserMock.Object,
            _loggerMock.Object);

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnAlbumNotFound_WhenAlbumDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        await SeedAsync(new Artist { UserId = userId, StageName = "Artist", Bio = "Bio" });

        var result = await BuildHandler().Handle(new PublishAlbumCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlbumAlreadyPublished_WhenAlbumIsPublished()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var artist = new Artist { UserId = userId, StageName = "Artist", Bio = "Bio" };
        await SeedAsync(artist);
        var album = new Album { Title = "Album", AlbumType = AlbumType.Album, IsPublished = true, TrackCount = 1, ReleaseDate = DateTime.UtcNow };
        await SeedAsync(album);
        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = artist.Id, Order = 0 });

        var result = await BuildHandler().Handle(new PublishAlbumCommand(album.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumAlreadyPublished);
    }

    [Fact]
    public async Task Handle_ShouldReturnCannotPublishEmptyAlbum_WhenAlbumHasNoTracks()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var artist = new Artist { UserId = userId, StageName = "Artist", Bio = "Bio" };
        await SeedAsync(artist);
        var album = new Album { Title = "Empty Album", AlbumType = AlbumType.Album, IsPublished = false, TrackCount = 0 };
        await SeedAsync(album);
        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = artist.Id, Order = 0 });

        var result = await BuildHandler().Handle(new PublishAlbumCommand(album.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.CannotPublishEmptyAlbum);
    }

    [Fact]
    public async Task Handle_ShouldPublishAlbum_WhenAllTracksHaveAvailableAudio()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var artist = new Artist { UserId = userId, StageName = "Artist", Bio = "Bio" };
        await SeedAsync(artist);
        var album = new Album { Title = "Ready Album", AlbumType = AlbumType.Album, IsPublished = false, TrackCount = 0 };
        await SeedAsync(album);
        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = artist.Id, Order = 0 });

        var track = new Track { AlbumId = album.Id, Title = "Track 1", DurationSeconds = 200, TrackNumber = 1 };
        await SeedAsync(track);
        await SeedAsync(new TrackFile { TrackId = track.Id, Status = FileStatus.Available });

        var result = await BuildHandler().Handle(new PublishAlbumCommand(album.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var publishedAlbum = await DbContext.Albums.AsNoTracking().FirstOrDefaultAsync(a => a.Id == album.Id);
        publishedAlbum!.IsPublished.Should().BeTrue();
        publishedAlbum.ReleaseDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenArtistIsNotPrimary()
    {
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupUser(otherUserId);

        var ownerArtist = new Artist { UserId = ownerUserId, StageName = "Owner", Bio = "Bio" };
        var otherArtist = new Artist { UserId = otherUserId, StageName = "Other", Bio = "Bio" };
        await SeedAsync(ownerArtist, otherArtist);

        var album = new Album { Title = "Album", AlbumType = AlbumType.Album, IsPublished = false, TrackCount = 1 };
        await SeedAsync(album);
        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = ownerArtist.Id, Order = 0 });

        var track = new Track { AlbumId = album.Id, Title = "Track 1", DurationSeconds = 200, TrackNumber = 1 };
        await SeedAsync(track);
        await SeedAsync(new TrackFile { TrackId = track.Id, Status = FileStatus.PendingUpload });

        var result = await BuildHandler().Handle(new PublishAlbumCommand(album.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedAlbumAccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(FileStatus.PendingUpload)]
    [InlineData(FileStatus.PendingValidation)]
    [InlineData(FileStatus.Processing)]
    [InlineData(FileStatus.Failed)]
    public async Task Handle_ShouldReturnTracksNotReady_WhenAnyTrackLacksAvailableAudio(FileStatus? notReadyStatus)
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var album = await SeedOwnedAlbumAsync(userId);

        var readyTrack = new Track { AlbumId = album.Id, Title = "Ready", TrackNumber = 1 };
        var notReadyTrack = new Track { AlbumId = album.Id, Title = "Not Ready", TrackNumber = 2 };
        await SeedAsync(readyTrack, notReadyTrack);
        await SeedAsync(new TrackFile { TrackId = readyTrack.Id, Status = FileStatus.Available });
        if (notReadyStatus is { } status)
            await SeedAsync(new TrackFile { TrackId = notReadyTrack.Id, Status = status });

        var result = await BuildHandler().Handle(new PublishAlbumCommand(album.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.TracksNotReady);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreSoftDeletedTracks_WhenCheckingReadiness()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var album = await SeedOwnedAlbumAsync(userId);

        var readyTrack = new Track { AlbumId = album.Id, Title = "Ready", TrackNumber = 1 };
        var deletedTrack = new Track { AlbumId = album.Id, Title = "Deleted", TrackNumber = 2, IsDeleted = true };
        await SeedAsync(readyTrack, deletedTrack);
        await SeedAsync(new TrackFile { TrackId = readyTrack.Id, Status = FileStatus.Available });

        var result = await BuildHandler().Handle(new PublishAlbumCommand(album.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnCannotPublishEmptyAlbum_WhenOnlySoftDeletedTracksRemain()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        var album = await SeedOwnedAlbumAsync(userId);

        var deletedTrack = new Track { AlbumId = album.Id, Title = "Deleted", TrackNumber = 1, IsDeleted = true };
        await SeedAsync(deletedTrack);
        await SeedAsync(new TrackFile { TrackId = deletedTrack.Id, Status = FileStatus.Available });

        var result = await BuildHandler().Handle(new PublishAlbumCommand(album.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.CannotPublishEmptyAlbum);
    }

    #endregion

    private async Task<Album> SeedOwnedAlbumAsync(Guid userId)
    {
        var artist = new Artist { UserId = userId, StageName = "Artist", Bio = "Bio" };
        await SeedAsync(artist);
        var album = new Album { Title = "Album", AlbumType = AlbumType.Album, IsPublished = false };
        await SeedAsync(album);
        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = artist.Id, Order = 0 });
        return album;
    }
}
