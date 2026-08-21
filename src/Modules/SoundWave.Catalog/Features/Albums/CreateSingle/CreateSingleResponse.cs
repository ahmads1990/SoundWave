namespace SoundWave.Catalog.Features.Albums.CreateSingle;

/// <summary>
/// Response returned after successfully creating a single release.
/// </summary>
public record CreateSingleResponse(Guid AlbumId, Guid TrackId);
