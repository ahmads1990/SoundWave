using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.GetNewReleases;
using SoundWave.Catalog.Features.ListAlbums;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models.Responses;
using System.Text.Json;

namespace SoundWave.Catalog.Tests.Features;

public class ListAlbumsTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<GetNewReleasesQueryHandler>> _newReleasesLoggerMock = new();
    private readonly Mock<ILogger<ListAlbumsQueryHandler>> _listLoggerMock = new();
    private readonly Mock<ICachingService> _cachingServiceMock = new();

    private GetNewReleasesQueryHandler BuildNewReleasesHandler()
        => new(CreateReadRepository<Album>(), _cachingServiceMock.Object, _newReleasesLoggerMock.Object);

    private ListAlbumsQueryHandler BuildListAlbumsHandler()
        => new(CreateReadRepository<Album>(), _cachingServiceMock.Object, _listLoggerMock.Object);

    private void SetupCacheMiss()
    {
        _cachingServiceMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _cachingServiceMock
            .Setup(x => x.AddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private async Task<Artist> SeedArtistWithAlbumsAsync()
    {
        var artist = new Artist { UserId = Guid.NewGuid(), StageName = "Artist One", Bio = "Bio" };
        await SeedAsync(artist);

        var albumA = new Album { Title = "Album A", AlbumType = AlbumType.Album, IsPublished = true, TrackCount = 1, ReleaseDate = DateTime.UtcNow.AddDays(-5) };
        var albumB = new Album { Title = "Album B", AlbumType = AlbumType.Single, IsPublished = true, TrackCount = 1, ReleaseDate = DateTime.UtcNow.AddDays(-1) };
        var albumC = new Album { Title = "Album C", AlbumType = AlbumType.EP, IsPublished = false, TrackCount = 0 };
        await SeedAsync(albumA, albumB, albumC);

        await SeedAsync(
            new AlbumArtist { AlbumId = albumA.Id, ArtistId = artist.Id, Order = 0 },
            new AlbumArtist { AlbumId = albumB.Id, ArtistId = artist.Id, Order = 0 },
            new AlbumArtist { AlbumId = albumC.Id, ArtistId = artist.Id, Order = 0 });

        return artist;
    }

    #region GetNewReleases Tests

    [Fact]
    public async Task GetNewReleases_ShouldReturnPublishedAlbums_OrderedByReleaseDateDesc()
    {
        SetupCacheMiss();
        await SeedArtistWithAlbumsAsync();

        var result = await BuildNewReleasesHandler().Handle(new GetNewReleasesQuery { PageIndex = 0, PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2); // Only published albums
        result.Data.Items.First().ReleaseDate.Should().BeAfter(result.Data.Items.Last().ReleaseDate!.Value);
    }

    [Fact]
    public async Task GetNewReleases_ShouldReturnCachedData_OnCacheHit()
    {
        var cachedResponse = new PaginatedResponse<AlbumSummaryDto>(
            [new(Guid.NewGuid(), "Cached Album", AlbumType.Album, DateTime.UtcNow, null, 1, [])],
            1, 0, 5);

        var cacheKey = Constants.Caching.GetNewReleasesKey(0, 5, null, null, null);
        _cachingServiceMock
            .Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedResponse));

        var result = await BuildNewReleasesHandler().Handle(new GetNewReleasesQuery { PageIndex = 0, PageSize = 5 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Title.Should().Be("Cached Album");
    }

    #endregion

    #region ListAlbums Tests

    [Fact]
    public async Task ListAlbums_ShouldReturnOnlyPublishedAlbums_WhenFilteredByIsPublishedTrue()
    {
        SetupCacheMiss();
        await SeedArtistWithAlbumsAsync();

        var query = new ListAlbumsQuery { IsPublished = true, PageIndex = 0, PageSize = 20 };
        var result = await BuildListAlbumsHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().AllSatisfy(a => a.IsPublished.Should().BeTrue());
        result.Data.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAlbums_ShouldFilterByTitle_CaseInsensitive()
    {
        SetupCacheMiss();
        await SeedArtistWithAlbumsAsync();

        var query = new ListAlbumsQuery { Title = "album a", PageIndex = 0, PageSize = 20 };
        var result = await BuildListAlbumsHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Title.Should().Be("Album A");
    }

    [Fact]
    public async Task ListAlbums_ShouldFilterByAlbumType()
    {
        SetupCacheMiss();
        await SeedArtistWithAlbumsAsync();

        var query = new ListAlbumsQuery { AlbumType = AlbumType.Single, IsPublished = true, PageIndex = 0, PageSize = 20 };
        var result = await BuildListAlbumsHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().AlbumType.Should().Be(AlbumType.Single);
    }

    [Fact]
    public async Task ListAlbums_ShouldPaginate_Correctly()
    {
        SetupCacheMiss();
        await SeedArtistWithAlbumsAsync();

        var query = new ListAlbumsQuery { IsPublished = true, PageIndex = 0, PageSize = 1, SortDirection = SortingDirection.Ascending };
        var result = await BuildListAlbumsHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.TotalCount.Should().Be(2);
        result.Data.TotalPages.Should().Be(2);
    }

    #endregion

    #region ListAlbumsRequestValidator Tests

    [Fact]
    public void Validator_ShouldFail_WhenPageIndexIsNegative()
    {
        var validator = new ListAlbumsRequestValidator();
        var request = new ListAlbumsRequest { PageIndex = -1, PageSize = 20 };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldFail_WhenOrderByIsInvalid()
    {
        var validator = new ListAlbumsRequestValidator();
        var request = new ListAlbumsRequest { PageIndex = 0, PageSize = 20, OrderBy = "invalid_field" };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var validator = new ListAlbumsRequestValidator();
        var request = new ListAlbumsRequest { PageIndex = 0, PageSize = 20, OrderBy = "Title" };
        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
