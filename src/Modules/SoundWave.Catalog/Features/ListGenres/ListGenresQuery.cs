using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.ListGenres;

internal record ListGenresQuery : BasePaginatedQuery, IRequest<Result<CatalogError, PaginatedResponse<ListGenreDto>>>
{
    public string? Name { get; init; }
    public GenreType? Type { get; init; }
}
