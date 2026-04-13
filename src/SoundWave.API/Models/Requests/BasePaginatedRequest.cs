using SoundWave.API.Common;

namespace SoundWave.API.Models.Requests;

/// <summary>
/// Base class for paginated API requests.
/// </summary>
public class BasePaginatedRequest
{
    public int PageIndex { get; set; } = Constants.DefaultPageIndex;
    public int PageSize { get; set; } = Constants.DefaultPageSize;
    public string? SortBy { get; set; }
    public SortingDirection SortDirection { get; set; } = SortingDirection.Ascending;
}
