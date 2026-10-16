namespace SoundWave.SharedKernel.Storage;

/// <summary>
/// Single source of truth for all S3 object storage key paths.
/// Enforces directory conventions, public vs private namespace isolation, and collision prevention.
/// </summary>
public static class StorageKeyGenerator
{
    #region Public Images

    /// <summary>User profile picture (avatar).</summary>
    /// <param name="fileId">The unique file identifier.</param>
    public static string ProfilePicture(Guid fileId)
        => $"public/images/avatars/{fileId:N}";

    /// <summary>User profile banner header.</summary>
    /// <param name="fileId">The unique file identifier.</param>
    public static string ProfileBanner(Guid fileId)
        => $"public/images/banners/profiles/{fileId:N}";

    /// <summary>Album cover art.</summary>
    /// <param name="fileId">The unique file identifier.</param>
    public static string AlbumCover(Guid fileId)
        => $"public/images/covers/albums/{fileId:N}";

    /// <summary>Playlist cover art.</summary>
    /// <param name="fileId">The unique file identifier.</param>
    public static string PlaylistCover(Guid fileId)
        => $"public/images/covers/playlists/{fileId:N}";

    #endregion

    #region Audio Raw Uploads (Private)

    /// <summary>
    /// Raw uploaded audio file before FFmpeg transcoding.
    /// Each upload attempt gets its own file id, so re-uploads never overwrite a previous object.
    /// Extension comes from original upload (e.g. ".mp3", ".flac").
    /// </summary>
    /// <param name="trackId">The unique track identifier.</param>
    /// <param name="fileId">The unique file identifier of this upload attempt.</param>
    /// <param name="extension">The raw audio file extension.</param>
    public static string RawAudio(Guid trackId, Guid fileId, string extension)
        => $"audio/raw/{trackId:N}/{fileId:N}{NormalizeExtension(extension)}";

    #endregion

    #region Audio HLS Output

    /// <summary>Full-length HLS master playlist (.m3u8).</summary>
    /// <param name="trackId">The unique track identifier.</param>
    public static string HlsPlaylist(Guid trackId)
        => $"audio/hls/{trackId:N}/playlist.m3u8";

    /// <summary>HLS segment file (.ts).</summary>
    /// <param name="trackId">The unique track identifier.</param>
    /// <param name="segmentIndex">The sequential segment index.</param>
    public static string HlsSegment(Guid trackId, int segmentIndex)
        => $"audio/hls/{trackId:N}/seg_{segmentIndex}.ts";

    /// <summary>30-second preview HLS playlist (.m3u8).</summary>
    /// <param name="trackId">The unique track identifier.</param>
    public static string PreviewPlaylist(Guid trackId)
        => $"audio/hls/{trackId:N}/preview.m3u8";

    /// <summary>HLS preview segment file (.ts).</summary>
    /// <param name="trackId">The unique track identifier.</param>
    /// <param name="segmentIndex">The sequential segment index.</param>
    public static string PreviewSegment(Guid trackId, int segmentIndex)
        => $"audio/hls/{trackId:N}/preview_seg_{segmentIndex}.ts";

    #endregion

    #region Audio Waveform

    /// <summary>Pre-generated waveform data (JSON peaks array).</summary>
    /// <param name="trackId">The unique track identifier.</param>
    public static string Waveform(Guid trackId)
        => $"audio/waveforms/{trackId:N}.json";

    #endregion

    #region Helpers

    /// <summary>
    /// Normalizes a file extension to ensure it is lowercased and prefixed with a single period.
    /// </summary>
    /// <param name="ext">The raw extension string.</param>
    /// <returns>The normalized extension string, or an empty string if null/whitespace.</returns>
    public static string NormalizeExtension(string ext)
    {
        if (string.IsNullOrWhiteSpace(ext)) return string.Empty;
        var trimmed = ext.Trim();
        return trimmed.StartsWith('.') ? trimmed.ToLowerInvariant() : $".{trimmed.ToLowerInvariant()}";
    }

    #endregion
}
