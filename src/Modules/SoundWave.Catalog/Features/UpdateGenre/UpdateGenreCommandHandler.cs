using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.UpdateGenre;

/// <summary>
/// Handles the update of an existing genre or mood in the catalog module.
/// </summary>
internal class UpdateGenreCommandHandler(
    CatalogDbContext dbContext,
    ILogger<UpdateGenreCommandHandler> logger)
    : IRequestHandler<UpdateGenreCommand, Result<CatalogError, int>>
{
    public async Task<Result<CatalogError, int>> Handle(UpdateGenreCommand request, CancellationToken cancellationToken)
    {
        var (genre, validation) = await GetAndValidateGenreAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return validation;

        await UpdateAndSaveGenreAsync(genre!, request, cancellationToken);
        return Result<CatalogError, int>.Success(genre!.Id);
    }

    #region Private Methods

    private async Task<(Genre? Genre, Result<CatalogError, int> Validation)> GetAndValidateGenreAsync(
        UpdateGenreCommand request,
        CancellationToken cancellationToken)
    {
        var genre = await dbContext.Genres.FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);
        if (genre == null)
        {
            logger.LogWarning("Genre update rejected — genre {GenreId} not found", request.Id);
            return (null, Result<CatalogError, int>.Failure(CatalogError.GenreNotFound));
        }

        var exists = await dbContext.Genres.AnyAsync(
            g => g.Id != request.Id && g.Name.ToLower() == request.Name.ToLower() && g.Type == request.Type,
            cancellationToken);

        if (exists)
        {
            logger.LogWarning("Genre update rejected — name {GenreName} of type {GenreType} already exists for another genre", request.Name, request.Type);
            return (null, Result<CatalogError, int>.Failure(CatalogError.GenreAlreadyExists));
        }

        return (genre, Result<CatalogError, int>.Success(genre.Id));
    }

    private async Task UpdateAndSaveGenreAsync(Genre genre, UpdateGenreCommand request, CancellationToken cancellationToken)
    {
        genre.Name = request.Name;
        genre.Type = request.Type;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Genre {GenreId} updated successfully to name {GenreName} and type {GenreType}", genre.Id, genre.Name, genre.Type);
    }

    #endregion
}
