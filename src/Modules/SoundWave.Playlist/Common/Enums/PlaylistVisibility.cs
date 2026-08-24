namespace SoundWave.Playlist.Common.Enums;

/// <summary>
/// Controls the accessibility and collaboration mode of a playlist.
/// </summary>
public enum PlaylistVisibility : byte
{
    /// <summary>
    /// Only visible to the owner.
    /// </summary>
    Private = 0,

    /// <summary>
    /// Visible to everyone and discoverable via search and user profiles.
    /// </summary>
    Public = 1,

    /// <summary>
    /// Can be viewed and modified by approved collaborators.
    /// </summary>
    Collaborative = 2
}
