using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Genres.UpdateGenre;

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
        var (genre, validationError) = await GetAndValidateAsync(request, cancellationToken);
        if (validationError != CatalogError.None)
            return Result<CatalogError, int>.Failure(validationError);

        await UpdateGenreAsync(genre!, request, cancellationToken);
        return Result<CatalogError, int>.Success(request.Id);
    }

    #region Private Methods

    private async Task<(Genre? Genre, CatalogError Error)> GetAndValidateAsync(
        UpdateGenreCommand request,
        CancellationToken cancellationToken)
    {
        var genre = await dbContext.Genres.FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);
        if (genre is null)
        {
            logger.LogWarning("Genre update rejected — genre {GenreId} not found", request.Id);
            return (null, CatalogError.GenreNotFound);
        }

        var isDuplicate = await dbContext.Genres.AnyAsync(
            g => g.Id != request.Id && g.Name.ToLower() == request.Name.ToLower() && g.Type == request.Type,
            cancellationToken);

        if (isDuplicate)
        {
            logger.LogWarning("Genre update rejected — name {GenreName} of type {GenreType} already exists for another genre", request.Name, request.Type);
            return (null, CatalogError.GenreAlreadyExists);
        }

        return (genre, CatalogError.None);
    }

    private async Task UpdateGenreAsync(Genre genre, UpdateGenreCommand request, CancellationToken cancellationToken)
    {
        genre.Name = request.Name;
        genre.Type = request.Type;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Genre {GenreId} updated successfully to name {GenreName} and type {GenreType}", genre.Id, genre.Name, genre.Type);
    }

    #endregion
}
