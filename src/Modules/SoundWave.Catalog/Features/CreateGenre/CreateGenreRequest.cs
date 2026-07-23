using SoundWave.Catalog.Common;

namespace SoundWave.Catalog.Features.CreateGenre;

/// <summary>
/// Request contract for creating a new genre or mood.
/// </summary>
/// <param name="Name">The display name of the genre or mood.</param>
/// <param name="Type">The type (Genre = 0, Mood = 1).</param>
internal record CreateGenreRequest(string Name, GenreType Type);
