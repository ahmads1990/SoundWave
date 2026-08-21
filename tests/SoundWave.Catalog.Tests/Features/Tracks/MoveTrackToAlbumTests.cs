using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Tracks.MoveTrackToAlbum;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Tracks;

public class MoveTrackToAlbumTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<MoveTrackToAlbumCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly MoveTrackToAlbumRequestValidator _validator = new();

    private MoveTrackToAlbumCommandHandler BuildHandler()
    {
        return new(
            CreateRepository<Track>(),
            CreateRepository<Album>(),
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
    public async Task Handle_ShouldReturnTrackNotFound_WhenTrackDoesNotExist()
    {
        SetupUser(Guid.NewGuid());
        var command = new MoveTrackToAlbumCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.TrackNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorizedTrackAccess_WhenUserIsNotPrimaryArtistOfTrack()
    {
        var primaryArtistUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupUser(otherUserId);

        var artist = new Artist { UserId = primaryArtistUserId, StageName = "Artist" };
        await SeedAsync(artist);

        var album = new Album
        {
            Title = "Album",
            AlbumArtists = [new AlbumArtist { ArtistId = artist.Id, Order = 0 }]
        };
        await SeedAsync(album);

        var track = new Track { Title = "Track 1", AlbumId = album.Id, TrackNumber = 1 };
        await SeedAsync(track);

        var command = new MoveTrackToAlbumCommand(track.Id, Guid.NewGuid());
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedTrackAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlbumNotFound_WhenTargetAlbumDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var artist = new Artist { UserId = userId, StageName = "Artist" };
        await SeedAsync(artist);

        var album = new Album
        {
            Title = "Source Album",
            AlbumArtists = [new AlbumArtist { ArtistId = artist.Id, Order = 0 }]
        };
        await SeedAsync(album);

        var track = new Track { Title = "Track 1", AlbumId = album.Id, TrackNumber = 1 };
        await SeedAsync(track);

        var command = new MoveTrackToAlbumCommand(track.Id, Guid.NewGuid());
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorizedAlbumAccess_WhenUserIsNotPrimaryArtistOfTargetAlbum()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupUser(userId);

        var myArtist = new Artist { UserId = userId, StageName = "My Artist" };
        var otherArtist = new Artist { UserId = otherUserId, StageName = "Other Artist" };
        await SeedAsync(myArtist);
        await SeedAsync(otherArtist);

        var sourceAlbum = new Album
        {
            Title = "Source Album",
            AlbumArtists = [new AlbumArtist { ArtistId = myArtist.Id, Order = 0 }]
        };
        var targetAlbum = new Album
        {
            Title = "Target Album Owned By Someone Else",
            AlbumArtists = [new AlbumArtist { ArtistId = otherArtist.Id, Order = 0 }]
        };
        await SeedAsync(sourceAlbum);
        await SeedAsync(targetAlbum);

        var track = new Track { Title = "Track 1", AlbumId = sourceAlbum.Id, TrackNumber = 1 };
        await SeedAsync(track);

        var command = new MoveTrackToAlbumCommand(track.Id, targetAlbum.Id);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedAlbumAccess);
    }

    [Fact]
    public async Task Handle_ShouldMoveTrack_AndReorderBothAlbums_AndPreserveMetadata()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var artist = new Artist { UserId = userId, StageName = "Artist" };
        await SeedAsync(artist);

        // Source album with 2 tracks
        var sourceAlbum = new Album
        {
            Title = "Source Album",
            TrackCount = 2,
            AlbumArtists = [new AlbumArtist { ArtistId = artist.Id, Order = 0 }]
        };
        // Target album with 1 track
        var targetAlbum = new Album
        {
            Title = "Target Album",
            TrackCount = 1,
            AlbumArtists = [new AlbumArtist { ArtistId = artist.Id, Order = 0 }]
        };
        await SeedAsync(sourceAlbum);
        await SeedAsync(targetAlbum);

        var sourceTrack1 = new Track { Title = "Source Track 1", AlbumId = sourceAlbum.Id, TrackNumber = 1 };
        var sourceTrack2 = new Track { Title = "Track To Move", AlbumId = sourceAlbum.Id, TrackNumber = 2, DurationSeconds = 240, PlayCount = 100 };
        var targetTrack1 = new Track { Title = "Target Track 1", AlbumId = targetAlbum.Id, TrackNumber = 1 };
        await SeedAsync(sourceTrack1);
        await SeedAsync(sourceTrack2);
        await SeedAsync(targetTrack1);

        // Move sourceTrack2 to targetAlbum
        var command = new MoveTrackToAlbumCommand(sourceTrack2.Id, targetAlbum.Id);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(sourceTrack2.Id);

        // Verify moved track
        var movedTrack = await DbContext.Tracks.FirstOrDefaultAsync(t => t.Id == sourceTrack2.Id);
        movedTrack.Should().NotBeNull();
        movedTrack!.AlbumId.Should().Be(targetAlbum.Id);
        movedTrack.TrackNumber.Should().Be(2); // Was placed as #2 in target album
        movedTrack.DurationSeconds.Should().Be(240); // Preserved
        movedTrack.PlayCount.Should().Be(100); // Preserved

        // Verify source album
        var updatedSource = await DbContext.Albums.FirstOrDefaultAsync(a => a.Id == sourceAlbum.Id);
        updatedSource.Should().NotBeNull();
        updatedSource!.TrackCount.Should().Be(1);

        // Verify target album
        var updatedTarget = await DbContext.Albums.FirstOrDefaultAsync(a => a.Id == targetAlbum.Id);
        updatedTarget.Should().NotBeNull();
        updatedTarget!.TrackCount.Should().Be(2);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenTargetAlbumIdIsEmpty()
    {
        var request = new MoveTrackToAlbumRequest(Guid.Empty);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetAlbumId");
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new MoveTrackToAlbumRequest(Guid.NewGuid());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
