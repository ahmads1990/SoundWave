namespace SoundWave.Catalog.Features.Tracks.MoveTrackToAlbum;

/// <summary>
/// Request DTO for moving a track to a different album.
/// </summary>
internal record MoveTrackToAlbumRequest(Guid TargetAlbumId);
