using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Tracks.EditTrackMetadata;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Tracks;

public class EditTrackMetadataTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<EditTrackMetadataCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly EditTrackMetadataRequestValidator _validator = new();

    private EditTrackMetadataCommandHandler BuildHandler()
    {
        var genreQueryHandler = new ValidateGenresExistQueryHandler(CreateReadDbContext());
        _senderMock.Setup(s => s.Send(It.IsAny<ValidateGenresExistQuery>(), It.IsAny<CancellationToken>()))
            .Returns<IRequest<bool>, CancellationToken>((q, ct) => genreQueryHandler.Handle((ValidateGenresExistQuery)q, ct));

        return new(
            CreateRepository<Track>(),
            _senderMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    private async Task<(Artist Artist, Album Album, Track Track)> SeedAlbumAndTrackAsync(Guid userId)
    {
        var artist = new Artist { UserId = userId, StageName = "Primary Artist", Bio = "Bio" };
        await SeedAsync(artist);

        var album = new Album { Title = "Test Album", AlbumType = AlbumType.Album, IsPublished = false, TrackCount = 1 };
        await SeedAsync(album);

        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = artist.Id, Order = 0 });

        var track = new Track { AlbumId = album.Id, Title = "Original Title", DurationSeconds = 180, TrackNumber = 1 };
        await SeedAsync(track);

        await SeedAsync(new TrackArtist { TrackId = track.Id, ArtistId = artist.Id, Order = 0 });

        return (artist, album, track);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnTrackNotFound_WhenTrackDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);
        await SeedAsync(new Artist { UserId = userId, StageName = "Artist", Bio = "Bio" });

        var command = new EditTrackMetadataCommand(Guid.NewGuid(), "Updated Title", 200, null, null);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.TrackNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotPrimaryArtist()
    {
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupUser(otherUserId);

        var (_, _, track) = await SeedAlbumAndTrackAsync(ownerUserId);

        var otherArtist = new Artist { UserId = otherUserId, StageName = "Other Artist", Bio = "Bio" };
        await SeedAsync(otherArtist);

        var command = new EditTrackMetadataCommand(track.Id, "Hacked Title", 200, null, null);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedTrackAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidGenreId_WhenGenreDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var (_, _, track) = await SeedAlbumAndTrackAsync(userId);

        var command = new EditTrackMetadataCommand(track.Id, "Updated Title", 200, [999], null);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.InvalidGenreId);
    }

    [Fact]
    public async Task Handle_ShouldUpdateTrackMetadata_AndSyncFeaturedArtistsAndGenres()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var (primaryArtist, _, track) = await SeedAlbumAndTrackAsync(userId);

        var featuredArtist = new Artist { UserId = Guid.NewGuid(), StageName = "Featured", Bio = "Bio" };
        await SeedAsync(featuredArtist);

        var genre = new Genre { Name = "Electronic", Type = GenreType.Genre };
        await SeedAsync(genre);

        var command = new EditTrackMetadataCommand(
            track.Id,
            "Remixed Title",
            240,
            [genre.Id],
            [featuredArtist.Id]);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(track.Id);

        var updatedTrack = await DbContext.Tracks
            .Include(t => t.TrackArtists)
            .Include(t => t.TrackGenres)
            .FirstOrDefaultAsync(t => t.Id == track.Id);

        updatedTrack.Should().NotBeNull();
        updatedTrack!.Title.Should().Be("Remixed Title");
        updatedTrack.DurationSeconds.Should().Be(240);
        updatedTrack.TrackGenres.Should().HaveCount(1);
        updatedTrack.TrackGenres.First().GenreId.Should().Be(genre.Id);

        updatedTrack.TrackArtists.Should().HaveCount(2);
        updatedTrack.TrackArtists.Should().Contain(ta => ta.ArtistId == primaryArtist.Id && ta.Order == 0);
        updatedTrack.TrackArtists.Should().Contain(ta => ta.ArtistId == featuredArtist.Id && ta.Order == 1);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenTitleIsEmpty()
    {
        var request = new EditTrackMetadataRequest("", 180, null, null);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validator_ShouldFail_WhenDurationIsNegative()
    {
        var request = new EditTrackMetadataRequest("Valid Title", -1, null, null);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DurationSeconds");
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new EditTrackMetadataRequest("Valid Title", 200, null, null);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
