using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Albums.UploadAlbumCover;

/// <summary>
/// Command to upload or replace the cover artwork of an album.
/// Only the primary artist of the album is authorized to invoke this command.
/// </summary>
/// <param name="AlbumId">The unique identifier of the album.</param>
/// <param name="Content">The image file stream.</param>
/// <param name="FileName">The original file name submitted by the client.</param>
/// <param name="ContentLength">The length of the payload in bytes.</param>
internal record UploadAlbumCoverCommand(
    Guid AlbumId,
    Stream Content,
    string FileName,
    long ContentLength) : IRequest<Result<CatalogError, UploadImageResponse>>;
