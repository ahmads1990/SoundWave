using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.CreateGenre;

/// <summary>
/// Handles the creation of a new genre or mood in the catalog module.
/// </summary>
internal class CreateGenreCommandHandler(
    CatalogDbContext dbContext,
    ILogger<CreateGenreCommandHandler> logger)
    : IRequestHandler<CreateGenreCommand, Result<CatalogError, int>>
{
    public async Task<Result<CatalogError, int>> Handle(CreateGenreCommand request, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return validation;

        var genre = await CreateAndSaveGenreAsync(request, cancellationToken);
        return Result<CatalogError, int>.Success(genre.Id);
    }

    #region Private Methods

    private async Task<Result<CatalogError, int>> ValidateAsync(CreateGenreCommand request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Genres.AnyAsync(
            g => g.Name.ToLower() == request.Name.ToLower() && g.Type == request.Type,
            cancellationToken);

        if (exists)
        {
            logger.LogWarning("Genre creation rejected — name {GenreName} of type {GenreType} already exists", request.Name, request.Type);
            return Result<CatalogError, int>.Failure(CatalogError.GenreAlreadyExists);
        }

        return Result<CatalogError, int>.Success(default);
    }

    private async Task<Genre> CreateAndSaveGenreAsync(CreateGenreCommand request, CancellationToken cancellationToken)
    {
        var genre = new Genre
        {
            Name = request.Name,
            Type = request.Type
        };

        await dbContext.Genres.AddAsync(genre, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Genre {GenreName} ({GenreId}) created successfully", genre.Name, genre.Id);
        return genre;
    }

    #endregion
}
