using MediatR;
using SoundWave.Playlist.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.UploadPlaylistCover;

/// <summary>
/// Command to upload or replace the cover artwork of a user-owned playlist.
/// </summary>
/// <param name="PlaylistId">The unique identifier of the playlist.</param>
/// <param name="Content">The image file stream.</param>
/// <param name="FileName">The original file name submitted by the client.</param>
/// <param name="ContentLength">The length of the payload in bytes.</param>
internal record UploadPlaylistCoverCommand(
    Guid PlaylistId,
    Stream Content,
    string FileName,
    long ContentLength) : IRequest<Result<PlaylistError, UploadImageResponse>>;
