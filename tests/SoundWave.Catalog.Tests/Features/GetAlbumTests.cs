using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.GetAlbum;

namespace SoundWave.Catalog.Tests.Features;

public class GetAlbumTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<GetAlbumQueryHandler>> _loggerMock = new();

    private GetAlbumQueryHandler BuildHandler()
        => new(CreateReadRepository<Album>(), _loggerMock.Object);

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnAlbumNotFound_WhenAlbumDoesNotExist()
    {
        var result = await BuildHandler().Handle(new GetAlbumQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.AlbumNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlbumWithOrderedTracks_FromDatabase()
    {
        var artist = new Artist { UserId = Guid.NewGuid(), StageName = "Test Artist", Bio = "Bio" };
        await SeedAsync(artist);

        var album = new Album { Title = "Test Album", AlbumType = AlbumType.Album, IsPublished = true, TrackCount = 2, ReleaseDate = DateTime.UtcNow };
        await SeedAsync(album);
        await SeedAsync(new AlbumArtist { AlbumId = album.Id, ArtistId = artist.Id, Order = 0 });

        var track1 = new Track { AlbumId = album.Id, Title = "Track A", DurationSeconds = 180, TrackNumber = 1 };
        var track2 = new Track { AlbumId = album.Id, Title = "Track B", DurationSeconds = 200, TrackNumber = 2 };
        await SeedAsync(track1, track2);
        await SeedAsync(new TrackFile { TrackId = track1.Id, Status = TrackFileStatus.Pending });
        await SeedAsync(new TrackFile { TrackId = track2.Id, Status = TrackFileStatus.Pending });
        await SeedAsync(
            new TrackArtist { TrackId = track1.Id, ArtistId = artist.Id, Order = 0 },
            new TrackArtist { TrackId = track2.Id, ArtistId = artist.Id, Order = 0 });

        var result = await BuildHandler().Handle(new GetAlbumQuery(album.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Test Album");
        result.Data.Tracks.Should().HaveCount(2);
        result.Data.Tracks[0].TrackNumber.Should().Be(1);
        result.Data.Tracks[1].TrackNumber.Should().Be(2);
        result.Data.Artists.Should().HaveCount(1);
    }

    #endregion
}
