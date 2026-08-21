using MediatR;

namespace SoundWave.Catalog.Features.ValidateGenresExist;

/// <summary>
/// Internal query to validate that all specified genre IDs exist in the catalog.
/// </summary>
internal record ValidateGenresExistQuery(IReadOnlyList<int> GenreIds) : IRequest<bool>;
