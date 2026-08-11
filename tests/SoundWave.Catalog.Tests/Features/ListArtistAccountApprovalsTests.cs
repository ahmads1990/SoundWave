using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.ListArtistAccountApprovals;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Tests.Features;

public class ListArtistAccountApprovalsTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<ListArtistAccountApprovalsQueryHandler>> _loggerMock = new();
    private readonly ListArtistAccountApprovalsRequestValidator _validator = new();

    private ListArtistAccountApprovalsQueryHandler BuildHandler()
    {
        return new ListArtistAccountApprovalsQueryHandler(CreateReadDbContext(), _loggerMock.Object);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnOnlyPendingApprovals_ByDefault()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        await SeedAsync(
            new ArtistAccountApproval
            {
                UserId = user1,
                StageName = "Pending Artist",
                Status = ArtistApprovalStatus.Pending
            },
            new ArtistAccountApproval
            {
                UserId = user2,
                StageName = "Approved Artist",
                Status = ArtistApprovalStatus.Approved
            },
            new ArtistAccountApproval
            {
                UserId = user3,
                StageName = "Rejected Artist",
                Status = ArtistApprovalStatus.Rejected
            });

        var query = new ListArtistAccountApprovalsQuery
        {
            Status = ArtistApprovalStatus.Pending,
            PageIndex = 0,
            PageSize = 10
        };
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().ContainSingle(a => a.StageName == "Pending Artist" && a.Status == ArtistApprovalStatus.Pending);
        result.Data.Items.Should().NotContain(a => a.StageName == "Approved Artist" || a.StageName == "Rejected Artist");
    }

    [Fact]
    public async Task Handle_ShouldFilterByStageName()
    {
        // Arrange
        await SeedAsync(
            new ArtistAccountApproval { UserId = Guid.NewGuid(), StageName = "Rockstar Alex", Status = ArtistApprovalStatus.Pending },
            new ArtistAccountApproval { UserId = Guid.NewGuid(), StageName = "DJ Beats", Status = ArtistApprovalStatus.Pending }
        );

        var query = new ListArtistAccountApprovalsQuery
        {
            StageName = "Alex",
            Status = ArtistApprovalStatus.Pending,
            PageIndex = 0,
            PageSize = 10
        };
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle(a => a.StageName == "Rockstar Alex");
        result.Data.Items.Should().NotContain(a => a.StageName == "DJ Beats");
    }

    [Fact]
    public async Task Handle_ShouldReturnAllStatuses_WhenStatusIsNull()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await SeedAsync(
            new ArtistAccountApproval
            {
                UserId = user1,
                StageName = "First Applicant",
                Status = ArtistApprovalStatus.Pending
            },
            new ArtistAccountApproval
            {
                UserId = user2,
                StageName = "Second Applicant",
                Status = ArtistApprovalStatus.Approved
            });

        var query = new ListArtistAccountApprovalsQuery
        {
            Status = null,
            PageIndex = 0,
            PageSize = 10
        };
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Data.Items.Should().Contain(a => a.StageName == "First Applicant");
        result.Data.Items.Should().Contain(a => a.StageName == "Second Applicant");
    }

    [Fact]
    public async Task Handle_ShouldPaginateResultsCorrectly()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            await SeedAsync(new ArtistAccountApproval
            {
                UserId = Guid.NewGuid(),
                StageName = $"Artist Batch {i}",
                Status = ArtistApprovalStatus.Pending
            });
        }

        var queryPage0 = new ListArtistAccountApprovalsQuery
        {
            Status = ArtistApprovalStatus.Pending,
            PageIndex = 0,
            PageSize = 2
        };
        var queryPage1 = new ListArtistAccountApprovalsQuery
        {
            Status = ArtistApprovalStatus.Pending,
            PageIndex = 1,
            PageSize = 2
        };
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

        // Items across pages should not intersect
        resultPage0.Data.Items.Select(x => x.Id).Intersect(resultPage1.Data.Items.Select(x => x.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldSortByStageNameDescending()
    {
        // Arrange
        await SeedAsync(
            new ArtistAccountApproval { UserId = Guid.NewGuid(), StageName = "Alpha Artist", Status = ArtistApprovalStatus.Pending },
            new ArtistAccountApproval { UserId = Guid.NewGuid(), StageName = "Omega Artist", Status = ArtistApprovalStatus.Pending }
        );

        var query = new ListArtistAccountApprovalsQuery
        {
            Status = ArtistApprovalStatus.Pending,
            PageIndex = 0,
            PageSize = 10,
            OrderBy = "StageName",
            SortDirection = SortingDirection.Descending
        };

        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var items = result.Data!.Items.Where(a => a.StageName is "Alpha Artist" or "Omega Artist").ToList();
        items[0].StageName.Should().Be("Omega Artist");
        items[1].StageName.Should().Be("Alpha Artist");
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenPageIndexIsNegative()
    {
        var request = new ListArtistAccountApprovalsRequest { PageIndex = -1, PageSize = 10 };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageIndex");
    }

    [Fact]
    public void Validator_ShouldFail_WhenPageSizeIsZeroOrOver100()
    {
        var requestZero = new ListArtistAccountApprovalsRequest { PageIndex = 0, PageSize = 0 };
        var requestOver = new ListArtistAccountApprovalsRequest { PageIndex = 0, PageSize = 101 };

        _validator.Validate(requestZero).IsValid.Should().BeFalse();
        _validator.Validate(requestOver).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldFail_WhenOrderByIsInvalid()
    {
        var request = new ListArtistAccountApprovalsRequest { OrderBy = "InvalidColumn" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderBy");
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new ListArtistAccountApprovalsRequest
        {
            Status = ArtistApprovalStatus.Approved,
            PageIndex = 0,
            PageSize = 25,
            OrderBy = nameof(ArtistAccountApproval.StageName)
        };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}

