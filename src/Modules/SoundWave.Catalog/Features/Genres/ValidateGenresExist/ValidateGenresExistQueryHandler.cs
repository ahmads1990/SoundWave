using MediatR;
using Microsoft.EntityFrameworkCore;
using SoundWave.Catalog.Data;

namespace SoundWave.Catalog.Features.Genres.ValidateGenresExist;

/// <summary>
/// Handles validating that all provided genre IDs exist in the catalog.
/// </summary>
internal class ValidateGenresExistQueryHandler(CatalogReadDbContext readDbContext)
    : IRequestHandler<ValidateGenresExistQuery, bool>
{
    public async Task<bool> Handle(
        ValidateGenresExistQuery request,
        CancellationToken cancellationToken)
    {
        if (request.GenreIds is null || request.GenreIds.Count == 0)
            return true;

        var distinctGenreIds = request.GenreIds.Distinct().ToList();
        var existingCount = await readDbContext.Genres
            .CountAsync(g => distinctGenreIds.Contains(g.Id), cancellationToken);

        return existingCount == distinctGenreIds.Count;
    }
}
