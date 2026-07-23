using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.UpdateGenre;

/// <summary>
/// Request contract for updating an existing genre or mood.
/// </summary>
/// <param name="Name">The display name of the genre or mood.</param>
/// <param name="Type">The type (Genre = 0, Mood = 1).</param>
internal record UpdateGenreRequest(string Name, GenreType Type);
