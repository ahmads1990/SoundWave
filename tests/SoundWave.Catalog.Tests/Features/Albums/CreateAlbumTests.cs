using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Albums.CreateAlbum;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Albums;

public class CreateAlbumTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<CreateAlbumCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly CreateAlbumRequestValidator _validator = new();

    private CreateAlbumCommandHandler BuildHandler()
    {
        var genreQueryHandler = new ValidateGenresExistQueryHandler(CreateReadDbContext());
        _senderMock.Setup(s => s.Send(It.IsAny<ValidateGenresExistQuery>(), It.IsAny<CancellationToken>()))
            .Returns<IRequest<bool>, CancellationToken>((q, ct) => genreQueryHandler.Handle((ValidateGenresExistQuery)q, ct));

        return new(
            CreateRepository<Album>(),
            CreateRepository<Artist>(),
            _senderMock.Object,
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
    public async Task Handle_ShouldReturnArtistNotFound_WhenNoArtistProfileExists()
    {
        SetupUser(Guid.NewGuid());
        var command = new CreateAlbumCommand("Album Title", AlbumType.Album, null, null, null, null, null);
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

        var command = new CreateAlbumCommand("Album Title", AlbumType.Album, null, null, null, [999], null);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.InvalidGenreId);
    }

    [Fact]
    public async Task Handle_ShouldCreateAlbum_WithArtistAndGenres()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var genre = new Genre { Name = "Rock", Type = GenreType.Genre };
        await SeedAsync(genre);
        await SeedAsync(new Artist { UserId = userId, StageName = "Rock Artist", Bio = "Bio" });

        var command = new CreateAlbumCommand(
            "Debut Album",
            AlbumType.Album,
            DateTime.UtcNow,
            "https://cover.jpg",
            "First album",
            [genre.Id],
            null);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);

        var savedAlbum = await DbContext.Albums
            .Include(a => a.AlbumArtists)
            .Include(a => a.AlbumGenres)
            .FirstOrDefaultAsync(a => a.Id == result.Data);

        savedAlbum.Should().NotBeNull();
        savedAlbum!.Title.Should().Be("Debut Album");
        savedAlbum.IsPublished.Should().BeFalse();
        savedAlbum.TrackCount.Should().Be(0);
        savedAlbum.AlbumArtists.Should().HaveCount(1);
        savedAlbum.AlbumArtists.First().Order.Should().Be(0);
        savedAlbum.AlbumGenres.Should().HaveCount(1);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenTitleIsEmpty()
    {
        var request = new CreateAlbumRequest("", AlbumType.Album, null, null, null, null, null);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenTitleExceeds200Characters()
    {
        var request = new CreateAlbumRequest(new string('A', 201), AlbumType.Album, null, null, null, null, null);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new CreateAlbumRequest("Valid Album", AlbumType.EP, null, null, null, null, null);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
