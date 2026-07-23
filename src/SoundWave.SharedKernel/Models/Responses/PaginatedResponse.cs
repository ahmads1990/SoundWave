namespace SoundWave.SharedKernel.Models.Responses;

public class PaginatedResponse<T>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex + 1 < TotalPages;
    public IReadOnlyCollection<T> Items { get; }

    public PaginatedResponse(IReadOnlyCollection<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
