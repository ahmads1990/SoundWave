using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.GetTrackUploadUrl;

/// <summary>
/// Command to generate a presigned upload URL for track audio.
/// </summary>
internal record GetTrackUploadUrlCommand(
    Guid TrackId,
    string FileName,
    long FileSizeBytes,
    string? ContentType = null)
    : IRequest<Result<CatalogError, TrackUploadUrlResponse>>;
