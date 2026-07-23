using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.UpdateGenre;

/// <summary>
/// Command for updating an existing genre or mood in the catalog.
/// </summary>
/// <param name="Id">The unique identifier of the genre or mood.</param>
/// <param name="Name">The display name of the genre or mood.</param>
/// <param name="Type">The type (Genre = 0, Mood = 1).</param>
internal record UpdateGenreCommand(int Id, string Name, GenreType Type) : IRequest<Result<CatalogError, int>>;
