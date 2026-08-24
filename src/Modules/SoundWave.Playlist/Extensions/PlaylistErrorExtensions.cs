using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Extensions;

internal static class PlaylistErrorExtensions
{
    public static ApiErrorCode ToApiErrorCode(this PlaylistError error) => error switch
    {
        PlaylistError.PlaylistNotFound        => ApiErrorCode.ResourceNotFound,
        PlaylistError.Unauthorized            => ApiErrorCode.Forbidden,
        PlaylistError.SystemPlaylistProtected => ApiErrorCode.Forbidden,
        PlaylistError.InvalidTrack            => ApiErrorCode.ValidationFailed,
        PlaylistError.TrackAlreadyInPlaylist  => ApiErrorCode.ValidationFailed,
        PlaylistError.TrackNotInPlaylist      => ApiErrorCode.ValidationFailed,
        PlaylistError.UserNotAuthenticated    => ApiErrorCode.Unauthorized,
        PlaylistError.ValidationFailed        => ApiErrorCode.ValidationFailed,
        _                                     => ApiErrorCode.InternalServerError
    };
}
