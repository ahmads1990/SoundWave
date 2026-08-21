namespace SoundWave.Catalog.Features.Albums.CreateSingle;

/// <summary>
/// Request DTO for creating a 1-step single release (Album + Track).
/// </summary>
public record CreateSingleRequest(
    string Title,
    DateTime? ReleaseDate = null,
    string? CoverImageUrl = null,
    string? Description = null,
    int DurationSeconds = 0,
    List<int>? GenreIds = null,
    List<Guid>? FeaturedArtistIds = null
);
