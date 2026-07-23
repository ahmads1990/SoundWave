using SoundWave.SharedKernel.Common;

namespace SoundWave.SharedKernel.Models;

public record BasePaginatedQuery
{
    public int PageIndex { get; init; } = SharedConstants.Pagination.DefaultPageIndex;
    public int PageSize { get; init; } = SharedConstants.Pagination.DefaultPageSize;
    public string? OrderBy { get; init; }
    public SortingDirection SortDirection { get; init; } = SortingDirection.Ascending;
}
