namespace SoundWave.Catalog.Features.Tracks.CreateTrack;

/// <summary>
/// Response returned after successfully creating a track.
/// </summary>
/// <param name="TrackId">Unique identifier of the created track.</param>
/// <param name="TrackNumber">Position number of the track within the album.</param>
public record CreateTrackResponse(
    Guid TrackId,
    int TrackNumber);
