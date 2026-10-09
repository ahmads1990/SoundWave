using System.Text.Json.Serialization;

namespace SoundWave.SharedKernel.Common;

/// <summary>
/// Defines user roles within the system.
/// </summary>
public enum UserRole
{
    /// <summary>Standard listener/consumer account.</summary>
    Listener,

    /// <summary>Content creator/artist account.</summary>
    Artist,

    /// <summary>System administrator account.</summary>
    Admin
}

/// <summary>
/// Specifies the sorting direction for paginated queries.
/// </summary>
public enum SortingDirection
{
    /// <summary>Ascending sort order.</summary>
    [JsonPropertyName("asc")]
    Ascending,

    /// <summary>Descending sort order.</summary>
    [JsonPropertyName("desc")]
    Descending
}

/// <summary>
/// Tracks the lifecycle status of an uploaded file.
/// </summary>
public enum FileStatus
{
    /// <summary>File record created; waiting for client to complete upload to storage.</summary>
    PendingUpload,

    /// <summary>Storage notified that file was created; awaiting validation completion.</summary>
    PendingValidation,

    /// <summary>File has passed validation and is available for streaming/use.</summary>
    Available,

    /// <summary>File validation or upload failed.</summary>
    Failed
}

/// <summary>
/// Categorizes the domain purpose and format rules of a stored file.
/// </summary>
public enum FileType : byte
{
    /// <summary>User avatar/profile picture.</summary>
    ProfilePic = 0,

    /// <summary>Album cover artwork.</summary>
    AlbumCover = 1,

    /// <summary>Audio track file.</summary>
    Track = 2,

    /// <summary>Video media file.</summary>
    Video = 3,

    /// <summary>User profile header banner.</summary>
    ProfileBanner = 4,

    /// <summary>Playlist cover artwork.</summary>
    PlaylistCover = 5
}