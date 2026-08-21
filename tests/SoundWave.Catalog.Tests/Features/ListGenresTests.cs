using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.ListGenres;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using System.Text.Json;

namespace SoundWave.Catalog.Tests.Features;

public class ListGenresTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<ListGenresQueryHandler>> _loggerMock = new();
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly ListGenresRequestValidator _validator = new();

    private ListGenresQueryHandler BuildHandler()
    {
        return new ListGenresQueryHandler(
            CreateReadDbContext(),
            _cachingServiceMock.Object,
            _loggerMock.Object);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnCachedList_WhenCacheHit()
    {
        // Arrange
        var cachedItems = new List<ListGenreDto>
        {
            new(1, "Cached Genre", GenreType.Genre)
        };
        var cachedResponse = new SoundWave.SharedKernel.Models.Responses.PaginatedResponse<ListGenreDto>(cachedItems, 1, 0, 10);

        _cachingServiceMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedResponse));

        var query = new ListGenresQuery { PageIndex = 0, PageSize = 10 };
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().ContainSingle(g => g.Name == "Cached Genre");

        _cachingServiceMock.Verify(x => x.AddAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFilterByNameAndType_WhenNotCached()
    {
        // Arrange
        _cachingServiceMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        await SeedAsync(
            new Genre { Name = "Electronic", Type = GenreType.Genre },
            new Genre { Name = "Energetic", Type = GenreType.Mood },
            new Genre { Name = "Acoustic", Type = GenreType.Genre }
        );

        var query = new ListGenresQuery
        {
            Name = "Electr",
            Type = GenreType.Genre,
            PageIndex = 0,
            PageSize = 10
        };
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle(g => g.Name == "Electronic");
        result.Data.Items.Should().NotContain(g => g.Name == "Energetic" || g.Name == "Acoustic");

        _cachingServiceMock.Verify(x => x.AddAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPaginateResultsCorrectly()
    {
        // Arrange
        _cachingServiceMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        for (int i = 1; i <= 5; i++)
        {
            await SeedAsync(new Genre { Name = $"Genre {i:D2}", Type = GenreType.Genre });
        }

        var queryPage0 = new ListGenresQuery { PageIndex = 0, PageSize = 2 };
        var queryPage1 = new ListGenresQuery { PageIndex = 1, PageSize = 2 };
        var handler = BuildHandler();

        // Act
        var resultPage0 = await handler.Handle(queryPage0, CancellationToken.None);
        var resultPage1 = await handler.Handle(queryPage1, CancellationToken.None);

        // Assert
        resultPage0.IsSuccess.Should().BeTrue();
        resultPage0.Data!.Items.Should().HaveCount(2);
        resultPage0.Data.TotalCount.Should().BeGreaterThanOrEqualTo(5);

        resultPage1.IsSuccess.Should().BeTrue();
        resultPage1.Data!.Items.Should().HaveCount(2);

        resultPage0.Data.Items.Select(x => x.Id).Intersect(resultPage1.Data.Items.Select(x => x.Id)).Should().BeEmpty();
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenPageIndexIsNegative()
    {
        var request = new ListGenresRequest { PageIndex = -1, PageSize = 10 };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageIndex");
    }

    [Fact]
    public void Validator_ShouldFail_WhenPageSizeIsZeroOrOver100()
    {
        var requestZero = new ListGenresRequest { PageIndex = 0, PageSize = 0 };
        var requestOver = new ListGenresRequest { PageIndex = 0, PageSize = 101 };

        _validator.Validate(requestZero).IsValid.Should().BeFalse();
        _validator.Validate(requestOver).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new ListGenresRequest
        {
            Name = "Rock",
            Type = GenreType.Genre,
            PageIndex = 0,
            PageSize = 25
        };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
