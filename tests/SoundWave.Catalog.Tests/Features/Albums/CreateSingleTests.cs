using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Albums.CreateAlbum;
using SoundWave.Catalog.Features.Albums.CreateSingle;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.Catalog.Features.Tracks.CreateTrack;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Albums;

public class CreateSingleTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<CreateSingleCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly CreateSingleRequestValidator _validator = new();

    private CreateSingleCommandHandler BuildHandler()
    {
        var genreQueryHandler = new ValidateGenresExistQueryHandler(CreateReadDbContext());
        var createAlbumHandler = new CreateAlbumCommandHandler(
            CreateRepository<Album>(),
            CreateRepository<Artist>(),
            _senderMock.Object,
            _currentUserMock.Object,
            Mock.Of<ILogger<CreateAlbumCommandHandler>>());
        var createTrackHandler = new CreateTrackCommandHandler(
            CreateRepository<Track>(),
            CreateRepository<Album>(),
            _senderMock.Object,
            _currentUserMock.Object,
            Mock.Of<ILogger<CreateTrackCommandHandler>>());

        _senderMock.Setup(s => s.Send(It.IsAny<ValidateGenresExistQuery>(), It.IsAny<CancellationToken>()))
            .Returns<IRequest<bool>, CancellationToken>((q, ct) => genreQueryHandler.Handle((ValidateGenresExistQuery)q, ct));

        _senderMock.Setup(s => s.Send(It.IsAny<CreateAlbumCommand>(), It.IsAny<CancellationToken>()))
            .Returns<IRequest<Result<CatalogError, Guid>>, CancellationToken>((cmd, ct) => createAlbumHandler.Handle((CreateAlbumCommand)cmd, ct));

        _senderMock.Setup(s => s.Send(It.IsAny<CreateTrackCommand>(), It.IsAny<CancellationToken>()))
            .Returns<IRequest<Result<CatalogError, Guid>>, CancellationToken>((cmd, ct) => createTrackHandler.Handle((CreateTrackCommand)cmd, ct));

        return new(_senderMock.Object, _loggerMock.Object);
    }

    private void SetupUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnArtistNotFound_WhenNoArtistProfileExists()
    {
        SetupUser(Guid.NewGuid());
        var command = new CreateSingleCommand("Single Title");
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.ArtistNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidGenreId_WhenGenreDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        await SeedAsync(new Artist { UserId = userId, StageName = "Test Artist", Bio = "Bio" });

        var command = new CreateSingleCommand("Single Title", GenreIds: [999]);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.InvalidGenreId);
    }

    [Fact]
    public async Task Handle_ShouldCreateSingleAlbumAndTrack_Atomically()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var genre = new Genre { Name = "Electronic", Type = GenreType.Genre };
        await SeedAsync(genre);

        var artist = new Artist { UserId = userId, StageName = "DJ Tester", Bio = "Producer" };
        var featuredArtist = new Artist { UserId = Guid.NewGuid(), StageName = "MC Feat", Bio = "Vocalist" };
        await SeedAsync(artist);
        await SeedAsync(featuredArtist);

        var command = new CreateSingleCommand(
            Title: "Midnight Pulse",
            ReleaseDate: DateTime.UtcNow,
            CoverImageUrl: "https://art.jpg",
            Description: "New single",
            DurationSeconds: 215,
            GenreIds: [genre.Id],
            FeaturedArtistIds: [featuredArtist.Id]);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.AlbumId.Should().NotBe(Guid.Empty);
        result.Data.TrackId.Should().NotBe(Guid.Empty);

        // Verify Album
        var savedAlbum = await DbContext.Albums
            .Include(a => a.Tracks)
                .ThenInclude(t => t.TrackFile)
            .Include(a => a.Tracks)
                .ThenInclude(t => t.TrackArtists)
            .Include(a => a.Tracks)
                .ThenInclude(t => t.TrackGenres)
            .Include(a => a.AlbumArtists)
            .Include(a => a.AlbumGenres)
            .FirstOrDefaultAsync(a => a.Id == result.Data.AlbumId);

        savedAlbum.Should().NotBeNull();
        savedAlbum!.Title.Should().Be("Midnight Pulse");
        savedAlbum.AlbumType.Should().Be(AlbumType.Single);
        savedAlbum.IsPublished.Should().BeFalse();
        savedAlbum.TrackCount.Should().Be(1);
        savedAlbum.AlbumArtists.Should().HaveCount(2);
        savedAlbum.AlbumArtists.First(aa => aa.ArtistId == artist.Id).Order.Should().Be(0);
        savedAlbum.AlbumArtists.First(aa => aa.ArtistId == featuredArtist.Id).Order.Should().Be(1);
        savedAlbum.AlbumGenres.Should().HaveCount(1);

        // Verify Track
        savedAlbum.Tracks.Should().HaveCount(1);
        var savedTrack = savedAlbum.Tracks.First();
        savedTrack.Id.Should().Be(result.Data.TrackId);
        savedTrack.Title.Should().Be("Midnight Pulse");
        savedTrack.DurationSeconds.Should().Be(215);
        savedTrack.TrackNumber.Should().Be(1);
        savedTrack.TrackArtists.Should().HaveCount(2);
        savedTrack.TrackArtists.First(ta => ta.ArtistId == artist.Id).Order.Should().Be(0);
        savedTrack.TrackArtists.First(ta => ta.ArtistId == featuredArtist.Id).Order.Should().Be(1);
        savedTrack.TrackGenres.Should().HaveCount(1);
        savedTrack.TrackFile.Should().NotBeNull();
        savedTrack.TrackFile!.Status.Should().Be(TrackFileStatus.Pending);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenTitleIsEmpty()
    {
        var request = new CreateSingleRequest("");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenTitleExceeds200Characters()
    {
        var request = new CreateSingleRequest(new string('X', 201));
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenDurationIsNegative()
    {
        var request = new CreateSingleRequest("Title", DurationSeconds: -5);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DurationSeconds");
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new CreateSingleRequest("Valid Single", DurationSeconds: 180);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
