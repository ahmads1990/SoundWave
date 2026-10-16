using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Storage;

namespace SoundWave.Catalog.Features.Tracks.GetTrackUploadUrl;

/// <summary>
/// Handles generating a presigned S3 PUT URL for uploading raw track audio.
/// Validates primary artist ownership, creates a new FileMetadata row per upload attempt,
/// supersedes any previous upload, and points the track's TrackFile at the new file.
/// </summary>
internal class GetTrackUploadUrlCommandHandler(
    ICatalogRepository<Track> trackRepository,
    IFileMetadataRepository fileMetadataRepository,
    IObjectStorageService objectStorageService,
    ICurrentUserService currentUserService,
    ILogger<GetTrackUploadUrlCommandHandler> logger)
    : IRequestHandler<GetTrackUploadUrlCommand, Result<CatalogError, TrackUploadUrlResponse>>
{
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(30);

    public async Task<Result<CatalogError, TrackUploadUrlResponse>> Handle(
        GetTrackUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, TrackUploadUrlResponse>.Failure(validation.Error, validation.ErrorMessage);

        var track = validation.Data!;
        var response = await PrepareUploadAsync(track, request, cancellationToken);
        return Result<CatalogError, TrackUploadUrlResponse>.Success(response);
    }

    #region Private Methods

    private async Task<Result<CatalogError, Track>> ValidateAsync(
        GetTrackUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var track = await trackRepository.GetAll()
            .Include(t => t.TrackFile)
            .Include(t => t.Album)
                .ThenInclude(a => a.AlbumArtists)
                    .ThenInclude(aa => aa.Artist)
            .FirstOrDefaultAsync(t => t.Id == request.TrackId && !t.IsDeleted, cancellationToken);

        if (track is null)
        {
            logger.LogWarning("Get track upload URL rejected — track {TrackId} not found", request.TrackId);
            return Result<CatalogError, Track>.Failure(CatalogError.TrackNotFound, $"Track '{request.TrackId}' not found.");
        }

        var isPrimaryArtist = track.Album.AlbumArtists.Any(aa => aa.Order == 0 && aa.Artist.UserId == userId);
        if (!isPrimaryArtist)
        {
            logger.LogWarning("Get track upload URL rejected — user {UserId} is not primary artist of track {TrackId}", userId, request.TrackId);
            return Result<CatalogError, Track>.Failure(CatalogError.UnauthorizedTrackAccess, "Only the primary artist can upload audio for this track.");
        }

        if (track.Album.IsPublished)
        {
            logger.LogWarning("Get track upload URL rejected — album {AlbumId} is already published", track.AlbumId);
            return Result<CatalogError, Track>.Failure(CatalogError.AlbumAlreadyPublished, "Cannot upload audio for tracks in an album that has already been published.");
        }

        return Result<CatalogError, Track>.Success(track);
    }

    private async Task<TrackUploadUrlResponse> PrepareUploadAsync(
        Track track,
        GetTrackUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        var fileId = Guid.CreateVersion7();
        var ext = StorageKeyGenerator.NormalizeExtension(Path.GetExtension(request.FileName));
        var storageKey = StorageKeyGenerator.RawAudio(track.Id, fileId, ext);

        var uploadUrl = await objectStorageService.GenerateUploadUrlAsync(
            fileId,
            storageKey,
            UploadUrlExpiry,
            request.ContentType,
            cancellationToken: cancellationToken);

        var fileMetadata = FileMetadata.CreatePendingUpload(
            fileId,
            storageKey,
            request.FileName,
            FileType.Track,
            request.ContentType ?? "audio/mpeg",
            request.FileSizeBytes);
        await fileMetadataRepository.AddAsync(fileMetadata, cancellationToken);

        if (track.TrackFile?.FileId is { } previousFileId)
            await SupersedePreviousFileAsync(track.Id, previousFileId, cancellationToken);

        track.TrackFile ??= new TrackFile { TrackId = track.Id };
        track.TrackFile.FileId = fileId;
        track.TrackFile.RawFilePath = storageKey;
        track.TrackFile.FileSizeBytes = request.FileSizeBytes;
        track.TrackFile.Status = FileStatus.PendingUpload;
        track.TrackFile.FailureReason = null;
        track.TrackFile.HlsPlaylistPath = null;
        track.TrackFile.PreviewPlaylistPath = null;
        track.TrackFile.ProcessingStartedAt = null;

        await trackRepository.SaveChanges(cancellationToken);

        logger.LogInformation("Generated presigned audio upload URL for track {TrackId} at key {StorageKey}", track.Id, storageKey);

        return new TrackUploadUrlResponse(
            track.Id,
            uploadUrl,
            storageKey,
            DateTime.UtcNow.Add(UploadUrlExpiry));
    }

    /// <summary>
    /// Soft-deletes the previous upload's FileMetadata row and tags its object as expired.
    /// The S3 lifecycle rule purges the object, and the ObjectRemoved notification hard-deletes the row.
    /// </summary>
    private async Task SupersedePreviousFileAsync(Guid trackId, Guid previousFileId, CancellationToken cancellationToken)
    {
        var previousFile = await fileMetadataRepository.GetByIdAsync(previousFileId, cancellationToken);
        if (previousFile is null || previousFile.IsDeleted)
            return;

        previousFile.MarkAsDeleted();
        await fileMetadataRepository.UpdateAsync(previousFile, cancellationToken);

        try
        {
            await objectStorageService.DeleteAsync(previousFile.StorageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            // The previous object may never have been uploaded — superseding must not fail the new upload
            logger.LogWarning(ex, "Failed to tag superseded audio {StorageKey} of track {TrackId} for expiry", previousFile.StorageKey, trackId);
        }

        logger.LogInformation("Superseded previous audio file {FileId} of track {TrackId}", previousFileId, trackId);
    }

    #endregion
}
