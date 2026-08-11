using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.GetArtistProfile;
using SoundWave.SharedKernel.Interfaces;
using System.Text.Json;

namespace SoundWave.Catalog.Tests.Features;

public class GetArtistProfileTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<GetArtistProfileQueryHandler>> _loggerMock = new();
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly GetArtistProfileRequestValidator _validator = new();

    private GetArtistProfileQueryHandler BuildHandler()
    {
        return new GetArtistProfileQueryHandler(
            CreateReadDbContext(),
            _cachingServiceMock.Object,
            _loggerMock.Object);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnCachedProfile_WhenCacheHit()
    {
        // Arrange
        var artistId = Guid.NewGuid();
        var cachedDto = new ArtistProfileDto(
            artistId,
            Guid.NewGuid(),
            "Cached Artist",
            "Bio from Redis",
            100,
            5000,
            25000,
            DateTime.UtcNow,
            [],
            []);

        var cacheKey = Constants.Caching.GetArtistProfileKey(artistId);
        _cachingServiceMock
            .Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedDto));

        var query = new GetArtistProfileQuery(artistId);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.StageName.Should().Be("Cached Artist");
        result.Data.Bio.Should().Be("Bio from Redis");

        // Verify DB was never hit for cache write
        _cachingServiceMock.Verify(x => x.AddAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnArtistNotFound_WhenArtistDoesNotExist()
    {
        // Arrange
        var artistId = Guid.NewGuid();
        var cacheKey = Constants.Caching.GetArtistProfileKey(artistId);
        _cachingServiceMock
            .Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var query = new GetArtistProfileQuery(artistId);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.ArtistNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFullProfileAndCache_WhenArtistExistsWithTracksAndAlbums()
    {
        // Arrange
        var artistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cacheKey = Constants.Caching.GetArtistProfileKey(artistId);

        _cachingServiceMock
            .Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var artist = new Artist
        {
            Id = artistId,
            UserId = userId,
            StageName = "Nova Beats",
            Bio = "Electronic soundscape producer",
            FollowerCount = 1200,
            MonthlyListeners = 45000,
            TotalStreams = 300000,
            ApprovedAt = DateTime.UtcNow
        };

        var publishedAlbum = new Album
        {
            Id = Guid.NewGuid(),
            Title = "Midnight Resonance",
            AlbumType = AlbumType.Album,
            IsPublished = true,
            ReleaseDate = DateTime.UtcNow.AddDays(-10),
            CoverImageUrl = "https://cdn.soundwave.io/covers/midnight.jpg",
            TrackCount = 2
        };

        var draftAlbum = new Album
        {
            Id = Guid.NewGuid(),
            Title = "Unreleased Gems",
            AlbumType = AlbumType.EP,
            IsPublished = false,
            TrackCount = 1
        };

        var track1 = new Track
        {
            Id = Guid.NewGuid(),
            AlbumId = publishedAlbum.Id,
            Album = publishedAlbum,
            Title = "Neon Skyline",
            DurationSeconds = 210,
            TrackNumber = 1,
            PlayCount = 50000,
            LikeCount = 1200
        };

        var track2 = new Track
        {
            Id = Guid.NewGuid(),
            AlbumId = publishedAlbum.Id,
            Album = publishedAlbum,
            Title = "Cyber City",
            DurationSeconds = 185,
            TrackNumber = 2,
            PlayCount = 95000,
            LikeCount = 2800
        };

        await SeedAsync(artist);
        await SeedAsync(publishedAlbum, draftAlbum);
        await SeedAsync(track1, track2);

        await SeedAsync(
            new AlbumArtist { AlbumId = publishedAlbum.Id, ArtistId = artistId },
            new AlbumArtist { AlbumId = draftAlbum.Id, ArtistId = artistId }
        );

        await SeedAsync(
            new TrackArtist { TrackId = track1.Id, ArtistId = artistId },
            new TrackArtist { TrackId = track2.Id, ArtistId = artistId }
        );

        var query = new GetArtistProfileQuery(artistId);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(artistId);
        result.Data.StageName.Should().Be("Nova Beats");
        result.Data.Bio.Should().Be("Electronic soundscape producer");

        // Top tracks should be ordered by PlayCount descending
        result.Data.TopTracks.Should().HaveCount(2);
        result.Data.TopTracks[0].Title.Should().Be("Cyber City");
        result.Data.TopTracks[0].PlayCount.Should().Be(95000);
        result.Data.TopTracks[1].Title.Should().Be("Neon Skyline");

        // Only published albums should be included
        result.Data.Albums.Should().HaveCount(1);
        result.Data.Albums[0].Title.Should().Be("Midnight Resonance");

        // Verify result was stored into Redis cache
        _cachingServiceMock.Verify(x => x.AddAsync(
            cacheKey,
            It.IsAny<string>(),
            It.Is<TimeSpan>(t => t.TotalMinutes == Constants.Caching.ArtistProfileTtlMinutes),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenArtistIdIsEmpty()
    {
        var request = new GetArtistProfileRequest(Guid.Empty);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenArtistIdIsValid()
    {
        var request = new GetArtistProfileRequest(Guid.NewGuid());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
