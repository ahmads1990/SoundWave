using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Tracks.DeleteTrack;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.Catalog.Tests.Features.Tracks;

public class DeleteTrackTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<DeleteTrackCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private DeleteTrackCommandHandler BuildHandler()
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

    [Fact]
    public async Task Handle_ShouldReturnTrackNotFound_WhenTrackDoesNotExist()
    {
        SetupUser(Guid.NewGuid());
        var command = new DeleteTrackCommand(Guid.NewGuid());
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.TrackNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorizedTrackAccess_WhenUserIsNotPrimaryArtist()
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

        var command = new DeleteTrackCommand(track.Id);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.UnauthorizedTrackAccess);
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteTrack_AndReGapRemainingTrackNumbers_AndUpdateTrackCount()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId);

        var artist = new Artist { UserId = userId, StageName = "Artist" };
        await SeedAsync(artist);

        var album = new Album
        {
            Title = "Album with 3 tracks",
            TrackCount = 3,
            AlbumArtists = [new AlbumArtist { ArtistId = artist.Id, Order = 0 }]
        };
        await SeedAsync(album);

        var track1 = new Track { Title = "Track 1", AlbumId = album.Id, TrackNumber = 1 };
        var track2 = new Track { Title = "Track 2", AlbumId = album.Id, TrackNumber = 2 };
        var track3 = new Track { Title = "Track 3", AlbumId = album.Id, TrackNumber = 3 };
        await SeedAsync(track1);
        await SeedAsync(track2);
        await SeedAsync(track3);

        // Delete track 2
        var command = new DeleteTrackCommand(track2.Id);
        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(track2.Id);

        // Verify soft delete
        var deletedTrack = await DbContext.Tracks.FirstOrDefaultAsync(t => t.Id == track2.Id);
        deletedTrack.Should().NotBeNull();
        deletedTrack!.IsDeleted.Should().BeTrue();

        // Verify remaining active tracks re-gapped (Track 1 is #1, Track 3 became #2)
        var remainingTracks = await DbContext.Tracks
            .Where(t => t.AlbumId == album.Id && !t.IsDeleted)
            .OrderBy(t => t.TrackNumber)
            .ToListAsync();

        remainingTracks.Should().HaveCount(2);
        remainingTracks[0].Id.Should().Be(track1.Id);
        remainingTracks[0].TrackNumber.Should().Be(1);
        remainingTracks[1].Id.Should().Be(track3.Id);
        remainingTracks[1].TrackNumber.Should().Be(2);

        // Verify Album TrackCount updated
        var updatedAlbum = await DbContext.Albums.FirstOrDefaultAsync(a => a.Id == album.Id);
        updatedAlbum.Should().NotBeNull();
        updatedAlbum!.TrackCount.Should().Be(2);
    }
}
