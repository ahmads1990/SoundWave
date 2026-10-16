using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Tracks.GetTrackStatus;

/// <summary>
/// Handles querying the current processing status and metadata for a track file.
/// Uses the read-only DbContext without tracking.
/// </summary>
internal class GetTrackStatusQueryHandler(
    CatalogReadDbContext readDbContext,
    ILogger<GetTrackStatusQueryHandler> logger)
    : IRequestHandler<GetTrackStatusQuery, Result<CatalogError, TrackStatusResponse>>
{
    public async Task<Result<CatalogError, TrackStatusResponse>> Handle(
        GetTrackStatusQuery request,
        CancellationToken cancellationToken)
    {
        var response = await FetchTrackStatusAsync(request.TrackId, cancellationToken);
        if (response is null)
        {
            logger.LogWarning("Get track status rejected — track {TrackId} not found", request.TrackId);
            return Result<CatalogError, TrackStatusResponse>.Failure(CatalogError.TrackNotFound, $"Track '{request.TrackId}' not found.");
        }

        return Result<CatalogError, TrackStatusResponse>.Success(response);
    }

    #region Private Methods

    private async Task<TrackStatusResponse?> FetchTrackStatusAsync(
        Guid trackId,
        CancellationToken cancellationToken)
    {
        return await readDbContext.Tracks
            .Where(t => t.Id == trackId && !t.IsDeleted)
            .Select(t => new TrackStatusResponse(
                t.Id,
                t.TrackFile != null ? t.TrackFile.Status : (FileStatus?)null,
                t.TrackFile != null ? t.TrackFile.FailureReason : null,
                t.TrackFile != null ? t.TrackFile.ProcessingStartedAt : null,
                t.TrackFile != null ? t.TrackFile.HlsPlaylistPath : null,
                t.TrackFile != null ? t.TrackFile.PreviewPlaylistPath : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    #endregion
}
