using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Albums.EditAlbum;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Albums;

public class EditAlbumTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<EditAlbumCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly EditAlbumRequestValidator _validator = new();

    private EditAlbumCommandHandler BuildHandler()
    {
        var genreQueryHandler = new ValidateGenresExistQueryHandler(CreateReadDbContext());
        _senderMock.Setup(s => s.Send(It.IsAny<ValidateGenresExistQuery>(), It.IsAny<CancellationToken>()))
            .Returns<IRequest<bool>, CancellationToken>((q, ct) => genreQueryHandler.Handle((ValidateGenresExistQuery)q, ct));

        return new(
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

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnAlbumNotFound_WhenAlbumDoesNotExist()
    {
        SetupUser(Guid.NewGuid());
        var command = new EditAlbumCommand(Guid.NewGuid(), "New Title", AlbumType.Album);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorizedAlbumAccess_WhenUserIsNotPrimaryArtist()
    {
        var primaryArtistUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupUser(otherUserId);

        var primaryArtist = new Artist { UserId = primaryArtistUserId, StageName = "Primary Artist" };
        await SeedAsync(primaryArtist);

        var album = new Album
        {
            Title = "Original Album",
            AlbumType = AlbumType.Album,
            AlbumArtists = [new AlbumArtist { ArtistId = primaryArtist.Id, Order = 0 }]
        };
        await SeedAsync(album);

        var command = new EditAlbumCommand(album.Id, "Updated Title", AlbumType.EP);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedAlbumAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidGenreId_WhenGenreDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var artist = new Artist { UserId = userId, StageName = "Artist" };
        await SeedAsync(artist);

        var album = new Album
        {
            Title = "Original Album",
            AlbumType = AlbumType.Album,
            AlbumArtists = [new AlbumArtist { ArtistId = artist.Id, Order = 0 }]
        };
        await SeedAsync(album);

        var command = new EditAlbumCommand(album.Id, "Updated Title", AlbumType.Album, GenreIds: [999]);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.InvalidGenreId);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAlbumMetadata_AndSyncGenresAndFeaturedArtists()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var primaryArtist = new Artist { UserId = userId, StageName = "Primary Artist" };
        var oldFeaturedArtist = new Artist { UserId = Guid.NewGuid(), StageName = "Old Feat" };
        var newFeaturedArtist = new Artist { UserId = Guid.NewGuid(), StageName = "New Feat" };
        await SeedAsync(primaryArtist);
        await SeedAsync(oldFeaturedArtist);
        await SeedAsync(newFeaturedArtist);

        var oldGenre = new Genre { Name = "Rock", Type = GenreType.Genre };
        var newGenre = new Genre { Name = "Pop", Type = GenreType.Genre };
        await SeedAsync(oldGenre);
        await SeedAsync(newGenre);

        var album = new Album
        {
            Title = "Original Title",
            AlbumType = AlbumType.Album,
            Description = "Old desc",
            CoverImageUrl = "https://old.jpg",
            AlbumArtists =
            [
                new AlbumArtist { ArtistId = primaryArtist.Id, Order = 0 },
                new AlbumArtist { ArtistId = oldFeaturedArtist.Id, Order = 1 }
            ],
            AlbumGenres = [new AlbumGenre { GenreId = oldGenre.Id }]
        };
        await SeedAsync(album);

        var releaseDate = DateTime.UtcNow;
        var command = new EditAlbumCommand(
            AlbumId: album.Id,
            Title: "Updated Album Title",
            AlbumType: AlbumType.EP,
            ReleaseDate: releaseDate,
            CoverImageUrl: "https://new.jpg",
            Description: "Updated desc",
            GenreIds: [newGenre.Id],
            FeaturedArtistIds: [newFeaturedArtist.Id]);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(album.Id);

        var updatedAlbum = await DbContext.Albums
            .Include(a => a.AlbumArtists)
            .Include(a => a.AlbumGenres)
            .FirstOrDefaultAsync(a => a.Id == album.Id);

        updatedAlbum.Should().NotBeNull();
        updatedAlbum!.Title.Should().Be("Updated Album Title");
        updatedAlbum.AlbumType.Should().Be(AlbumType.EP);
        updatedAlbum.CoverImageUrl.Should().Be("https://new.jpg");
        updatedAlbum.Description.Should().Be("Updated desc");
        updatedAlbum.ReleaseDate.Should().Be(releaseDate);

        // Verify genres synced
        updatedAlbum.AlbumGenres.Should().HaveCount(1);
        updatedAlbum.AlbumGenres.First().GenreId.Should().Be(newGenre.Id);

        // Verify artists synced: primary maintained (order 0), old replaced with new (order 1)
        updatedAlbum.AlbumArtists.Should().HaveCount(2);
        updatedAlbum.AlbumArtists.First(aa => aa.ArtistId == primaryArtist.Id).Order.Should().Be(0);
        updatedAlbum.AlbumArtists.First(aa => aa.ArtistId == newFeaturedArtist.Id).Order.Should().Be(1);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenTitleIsEmpty()
    {
        var request = new EditAlbumRequest("", AlbumType.Album);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenTitleExceeds200Characters()
    {
        var request = new EditAlbumRequest(new string('Z', 201), AlbumType.Album);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new EditAlbumRequest("Valid Updated Title", AlbumType.Album);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
