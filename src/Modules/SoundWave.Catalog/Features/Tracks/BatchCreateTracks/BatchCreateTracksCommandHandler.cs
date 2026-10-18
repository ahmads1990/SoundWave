using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Data.IRepository;
using SoundWave.Catalog.Features.Genres.ValidateGenresExist;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Storage;

namespace SoundWave.Catalog.Features.Tracks.BatchCreateTracks;

/// <summary>
/// Handles batch creation of tracks within an album and generates presigned S3 PUT URLs for each track's audio file.
/// Validates album ownership, permissions, genre IDs, and featured artists up front.
/// FileMetadata rows (SharedKernel context) are persisted first in a single save; orphans left by a later failure
/// stay in PendingUpload and are purged by the stale-upload cleanup job.
/// Track numbers are reserved and all tracks are committed in one Catalog transaction.
/// </summary>
internal class BatchCreateTracksCommandHandler(
    CatalogDbContext dbContext,
    ICatalogRepository<Track> trackRepository,
    ICatalogRepository<Album> albumRepository,
    ICatalogRepository<Artist> artistRepository,
    IFileMetadataRepository fileMetadataRepository,
    IObjectStorageService objectStorageService,
    ISender sender,
    ICurrentUserService currentUserService,
    ILogger<BatchCreateTracksCommandHandler> logger)
    : IRequestHandler<BatchCreateTracksCommand, Result<CatalogError, BatchCreateTracksResponse>>
{
    private const string DefaultAudioContentType = "audio/mpeg";
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(30);

    private sealed record PreparedUpload(
        BatchTrackItem Item,
        Guid TrackId,
        Guid FileId,
        string StorageKey,
        string UploadUrl);

    public async Task<Result<CatalogError, BatchCreateTracksResponse>> Handle(
        BatchCreateTracksCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsSuccess)
            return Result<CatalogError, BatchCreateTracksResponse>.Failure(validation.Error, validation.ErrorMessage);

        var uploads = await PrepareUploadsAsync(request, cancellationToken);
        return await CreateTracksAsync(request, validation.Data, uploads, cancellationToken);
    }

    #region Private Methods

    private async Task<Result<CatalogError, Guid>> ValidateAsync(
        BatchCreateTracksCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        var albumValidation = await albumRepository.GetAll()
            .Where(a => a.Id == request.AlbumId)
            .Select(a => new
            {
                a.IsPublished,
                PrimaryArtistId = a.AlbumArtists
                    .Where(aa => aa.Order == 0 && aa.Artist.UserId == userId)
                    .Select(aa => (Guid?)aa.ArtistId)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (albumValidation is null)
        {
            logger.LogWarning("Batch create tracks rejected — album {AlbumId} not found", request.AlbumId);
            return Result<CatalogError, Guid>.Failure(CatalogError.AlbumNotFound, $"Album '{request.AlbumId}' not found.");
        }

        if (albumValidation.PrimaryArtistId is null)
        {
            logger.LogWarning("Batch create tracks rejected — user {UserId} is not primary artist of album {AlbumId}", userId, request.AlbumId);
            return Result<CatalogError, Guid>.Failure(CatalogError.UnauthorizedAlbumAccess, "Only the primary artist can add tracks to this album.");
        }

        if (albumValidation.IsPublished)
        {
            logger.LogWarning("Batch create tracks rejected — album {AlbumId} is already published", request.AlbumId);
            return Result<CatalogError, Guid>.Failure(CatalogError.AlbumAlreadyPublished, "Cannot add tracks to an album that has already been published.");
        }

        var primaryArtistId = albumValidation.PrimaryArtistId.Value;

        // Validate genres across all tracks
        var allGenreIds = request.Tracks
            .Where(t => t.GenreIds is { Count: > 0 })
            .SelectMany(t => t.GenreIds!)
            .Distinct()
            .ToList();

        if (allGenreIds.Count > 0)
        {
            var allGenresExist = await sender.Send(new ValidateGenresExistQuery(allGenreIds), cancellationToken);
            if (!allGenresExist)
            {
                logger.LogWarning("Batch create tracks rejected — one or more genre IDs are invalid in album {AlbumId}", request.AlbumId);
                return Result<CatalogError, Guid>.Failure(CatalogError.InvalidGenreId, "One or more specified genre IDs do not exist.");
            }
        }

        // Validate featured artists across all tracks
        var allFeaturedArtistIds = request.Tracks
            .Where(t => t.FeaturedArtistIds is { Count: > 0 })
            .SelectMany(t => t.FeaturedArtistIds!)
            .Distinct()
            .Where(id => id != primaryArtistId)
            .ToList();

        if (allFeaturedArtistIds.Count > 0)
        {
            var existingArtistCount = await artistRepository.GetAll()
                .CountAsync(a => allFeaturedArtistIds.Contains(a.Id), cancellationToken);

            if (existingArtistCount != allFeaturedArtistIds.Count)
            {
                logger.LogWarning("Batch create tracks rejected — one or more featured artists do not exist in album {AlbumId}", request.AlbumId);
                return Result<CatalogError, Guid>.Failure(CatalogError.ArtistNotFound, "One or more specified featured artists do not exist.");
            }
        }

        return Result<CatalogError, Guid>.Success(primaryArtistId);
    }

    /// <summary>
    /// Generates a presigned URL per track and persists all pending FileMetadata rows in one save.
    /// Runs before the Catalog transaction so no storage I/O happens while the album row is locked.
    /// </summary>
    private async Task<List<PreparedUpload>> PrepareUploadsAsync(
        BatchCreateTracksCommand request,
        CancellationToken cancellationToken)
    {
        var uploads = new List<PreparedUpload>(request.Tracks.Count);
        var fileMetadata = new List<FileMetadata>(request.Tracks.Count);

        foreach (var item in request.Tracks)
        {
            var trackId = Guid.CreateVersion7();
            var fileId = Guid.CreateVersion7();
            var ext = StorageKeyGenerator.NormalizeExtension(Path.GetExtension(item.FileName));
            var storageKey = StorageKeyGenerator.RawAudio(trackId, fileId, ext);

            var uploadUrl = await objectStorageService.GenerateUploadUrlAsync(
                fileId,
                storageKey,
                UploadUrlExpiry,
                item.ContentType,
                cancellationToken: cancellationToken);

            fileMetadata.Add(FileMetadata.CreatePendingUpload(
                fileId,
                storageKey,
                item.FileName,
                FileType.Track,
                item.ContentType ?? DefaultAudioContentType,
                item.FileSizeBytes));

            uploads.Add(new PreparedUpload(item, trackId, fileId, storageKey, uploadUrl));
        }

        await fileMetadataRepository.AddRangeAsync(fileMetadata, cancellationToken);
        return uploads;
    }

    private async Task<Result<CatalogError, BatchCreateTracksResponse>> CreateTracksAsync(
        BatchCreateTracksCommand request,
        Guid primaryArtistId,
        List<PreparedUpload> uploads,
        CancellationToken cancellationToken)
    {
        // Join an ambient transaction if one exists (e.g. integration tests), otherwise own one
        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // Atomically reserve a contiguous block of track numbers. The UPDATE holds the album row lock
        // until commit, so concurrent batches serialize instead of reading the same TrackCount.
        var reserved = await albumRepository.GetAll()
            .Where(a => a.Id == request.AlbumId && !a.IsPublished)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.TrackCount, a => a.TrackCount + uploads.Count), cancellationToken);

        if (reserved == 0)
        {
            logger.LogWarning("Batch create tracks rejected — album {AlbumId} was published or deleted during the request", request.AlbumId);
            return Result<CatalogError, BatchCreateTracksResponse>.Failure(CatalogError.AlbumAlreadyPublished, "Cannot add tracks to an album that has already been published.");
        }

        var newTrackCount = await albumRepository.GetAll()
            .Where(a => a.Id == request.AlbumId)
            .Select(a => a.TrackCount)
            .FirstAsync(cancellationToken);

        var firstTrackNumber = newTrackCount - uploads.Count + 1;
        var expiresAt = DateTime.UtcNow.Add(UploadUrlExpiry);
        var tracks = new List<Track>(uploads.Count);
        var uploadItemResponses = new List<BatchTrackUploadItemResponse>(uploads.Count);

        for (var i = 0; i < uploads.Count; i++)
        {
            var upload = uploads[i];
            var item = upload.Item;
            var trackNumber = firstTrackNumber + i;

            var track = new Track
            {
                Id = upload.TrackId,
                AlbumId = request.AlbumId,
                Title = item.Title,
                TrackNumber = trackNumber,
                PlayCount = 0,
                LikeCount = 0,
                TrackFile = new TrackFile
                {
                    TrackId = upload.TrackId,
                    FileId = upload.FileId,
                    RawFilePath = upload.StorageKey,
                    FileSizeBytes = item.FileSizeBytes,
                    Status = FileStatus.PendingUpload
                }
            };

            // Primary artist (Order = 0)
            track.TrackArtists.Add(new TrackArtist { ArtistId = primaryArtistId, Order = 0 });

            // Featured artists
            if (item.FeaturedArtistIds is { Count: > 0 })
            {
                var order = 1;
                foreach (var featuredId in item.FeaturedArtistIds.Distinct().Where(id => id != primaryArtistId))
                {
                    track.TrackArtists.Add(new TrackArtist { ArtistId = featuredId, Order = order++ });
                }
            }

            // Genres
            if (item.GenreIds is { Count: > 0 })
            {
                foreach (var genreId in item.GenreIds.Distinct())
                {
                    track.TrackGenres.Add(new TrackGenre { GenreId = genreId });
                }
            }

            tracks.Add(track);

            uploadItemResponses.Add(new BatchTrackUploadItemResponse(
                upload.TrackId,
                trackNumber,
                track.Title,
                upload.UploadUrl,
                upload.StorageKey,
                expiresAt));
        }

        await trackRepository.AddRange(tracks, cancellationToken);
        await trackRepository.SaveChanges(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Batch created {TrackCount} tracks for album {AlbumId} starting at track #{StartNumber}", uploads.Count, request.AlbumId, firstTrackNumber);

        return Result<CatalogError, BatchCreateTracksResponse>.Success(
            new BatchCreateTracksResponse(request.AlbumId, uploadItemResponses));
    }

    #endregion
}
