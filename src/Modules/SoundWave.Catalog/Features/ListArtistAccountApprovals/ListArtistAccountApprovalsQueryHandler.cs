using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.ListArtistAccountApprovals;

/// <summary>
/// Handles retrieving paginated artist account applications/approvals with filtering and sorting.
/// </summary>
internal class ListArtistAccountApprovalsQueryHandler(
    CatalogReadDbContext dbContext,
    ILogger<ListArtistAccountApprovalsQueryHandler> logger)
    : IRequestHandler<ListArtistAccountApprovalsQuery, Result<CatalogError, PaginatedResponse<ListArtistAccountApprovalDto>>>
{
    public async Task<Result<CatalogError, PaginatedResponse<ListArtistAccountApprovalDto>>> Handle(
        ListArtistAccountApprovalsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Listing artist account approvals (Status: {Status}, StageName: {StageName}, PageIndex: {PageIndex}, PageSize: {PageSize})",
            request.Status, request.StageName, request.PageIndex, request.PageSize);

        var (items, totalCount) = await GetPaginatedApprovalsAsync(request, cancellationToken);
        var response = new PaginatedResponse<ListArtistAccountApprovalDto>(items, totalCount, request.PageIndex, request.PageSize);

        return Result<CatalogError, PaginatedResponse<ListArtistAccountApprovalDto>>.Success(response);
    }

    #region Private Methods

    private async Task<(List<ListArtistAccountApprovalDto> Items, int TotalCount)> GetPaginatedApprovalsAsync(
        ListArtistAccountApprovalsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ArtistAccountApprovals.AsQueryable();

        query = ApplySearchFilters(query, request);
        query = ApplySorting(query, request);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new ListArtistAccountApprovalDto(a.Id, a.UserId, a.StageName, a.Bio, a.Status, a.RejectionReason, a.ReviewedBy, a.ReviewedAt, a.CreatedDate))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<ArtistAccountApproval> ApplySearchFilters(
        IQueryable<ArtistAccountApproval> query,
        ListArtistAccountApprovalsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.StageName))
        {
            query = query.Where(a => EF.Functions.Like(a.StageName, $"%{request.StageName}%"));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        return query;
    }

    private static IQueryable<ArtistAccountApproval> ApplySorting(
        IQueryable<ArtistAccountApproval> query,
        ListArtistAccountApprovalsQuery request)
    {
        var isDescending = request.SortDirection == SortingDirection.Descending;

        return (request.OrderBy?.ToLower()) switch
        {
            "stagename" => isDescending ? query.OrderByDescending(a => a.StageName) : query.OrderBy(a => a.StageName),
            "status" => isDescending ? query.OrderByDescending(a => a.Status).ThenByDescending(a => a.CreatedDate) : query.OrderBy(a => a.Status).ThenByDescending(a => a.CreatedDate),
            "reviewedat" => isDescending ? query.OrderByDescending(a => a.ReviewedAt) : query.OrderBy(a => a.ReviewedAt),
            _ => isDescending ? query.OrderByDescending(a => a.CreatedDate) : query.OrderBy(a => a.CreatedDate)
        };
    }

    #endregion
}

