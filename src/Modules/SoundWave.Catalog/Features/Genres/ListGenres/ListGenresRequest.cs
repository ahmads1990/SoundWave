using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Models.Requests;

namespace SoundWave.Catalog.Features.Genres.ListGenres;

internal record ListGenresRequest : BasePaginatedRequest
{
    public string? Name { get; init; }
    public GenreType? Type { get; init; }

    public static readonly IReadOnlyList<string> AllowedSortFields = [nameof(Genre.Name), nameof(Genre.Type)];
}
