using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Tracks.CreateTrack;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Tracks;

public class CreateTrackTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<CreateTrackCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly CreateTrackRequestValidator _validator = new();

    private CreateTrackCommandHandler BuildHandler()
    {
        var genreQueryHandler = new ValidateGenresExistQueryHandler(CreateReadDbContext());
        _senderMock.Setup(s => s.Send(It.IsAny<ValidateGenresExistQuery>(), It.IsAny<CancellationToken>()))
            .Returns<IRequest<bool>, CancellationToken>((q, ct) => genreQueryHandler.Handle((ValidateGenresExistQuery)q, ct));

        return new(
            CreateRepository<Track>(),
            CreateRepository<Album>(),
            _senderMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    private async Task<(Artist Artist, Album Album)> SeedAlbumWithPrimaryArtistAsync(Guid userId)
    {
        var artist = new Artist { UserId = userId, StageName = "Test Artist", Bio = "Bio" };
        await SeedAsync(artist);

        var album = new Album { Title = "Test Album", AlbumType = AlbumType.Album, IsPublished = false, TrackCount = 0 };
        await SeedAsync(album);

        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = artist.Id, Order = 0 });

        return (artist, album);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnAlbumNotFound_WhenAlbumDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        await SeedAsync(new Artist { UserId = userId, StageName = "Artist", Bio = "Bio" });

        var command = new CreateTrackCommand(Guid.NewGuid(), "Track 1", 180, null, null);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumNotFound);
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

        var album = new Album { Title = "Album", AlbumType = AlbumType.Album, IsPublished = false, TrackCount = 0 };
        await SeedAsync(album);
        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = ownerArtist.Id, Order = 0 });

        var command = new CreateTrackCommand(album.Id, "Track 1", 180, null, null);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedAlbumAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidGenreId_WhenGenreDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var (_, album) = await SeedAlbumWithPrimaryArtistAsync(userId);

        var command = new CreateTrackCommand(album.Id, "Track 1", 180, [999], null);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.InvalidGenreId);
    }

    [Fact]
    public async Task Handle_ShouldAddTrack_WithAutoIncrementedTrackNumber()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var (_, album) = await SeedAlbumWithPrimaryArtistAsync(userId);

        var command1 = new CreateTrackCommand(album.Id, "Track One", 200, null, null);
        var result1 = await BuildHandler().Handle(command1, CancellationToken.None);

        var command2 = new CreateTrackCommand(album.Id, "Track Two", 210, null, null);
        var result2 = await BuildHandler().Handle(command2, CancellationToken.None);

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var track1 = await DbContext.Tracks.FindAsync(result1.Data);
        var track2 = await DbContext.Tracks.FindAsync(result2.Data);

        track1!.TrackNumber.Should().Be(1);
        track2!.TrackNumber.Should().Be(2);

        var updatedAlbum = await DbContext.Albums.FindAsync(album.Id);
        updatedAlbum!.TrackCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldCreatePendingTrackFile_WhenTrackIsAdded()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var (_, album) = await SeedAlbumWithPrimaryArtistAsync(userId);

        var command = new CreateTrackCommand(album.Id, "My Track", 180, null, null);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var track = await DbContext.Tracks
            .Include(t => t.TrackFile)
            .FirstOrDefaultAsync(t => t.Id == result.Data);

        track!.TrackFile.Should().NotBeNull();
        track.TrackFile!.Status.Should().Be(TrackFileStatus.Pending);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenTitleIsEmpty()
    {
        var request = new CreateTrackRequest("", 180, null, null);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validator_ShouldFail_WhenDurationIsNegative()
    {
        var request = new CreateTrackRequest("Track Title", -1, null, null);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DurationSeconds");
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new CreateTrackRequest("My Track", 200, null, null);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
