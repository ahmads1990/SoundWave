using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Genres.CreateGenre;

/// <summary>
/// Command for creating a new genre or mood in the catalog.
/// </summary>
/// <param name="Name">The display name of the genre or mood.</param>
/// <param name="Type">The type (Genre = 0, Mood = 1).</param>
internal record CreateGenreCommand(string Name, GenreType Type) : IRequest<Result<CatalogError, int>>;
