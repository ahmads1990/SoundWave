using Microsoft.Extensions.Logging;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Entities;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Validation;

namespace SoundWave.SharedKernel.Storage.Services;

/// <summary>
/// Default implementation of <see cref="IImageUploadService"/>.
/// Validates stream signatures via <see cref="FileMetadataValidator"/>, records metadata,
/// and synchronizes uploads and soft-deletes with object storage.
/// </summary>
public class ImageUploadService : IImageUploadService
{
    private readonly FileMetadataValidator _validator;
    private readonly IObjectStorageService _storageService;
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly ILogger<ImageUploadService> _logger;

    public ImageUploadService(
        FileMetadataValidator validator,
        IObjectStorageService storageService,
        IFileMetadataRepository fileMetadataRepository,
        ILogger<ImageUploadService> logger)
    {
        _validator = validator;
        _storageService = storageService;
        _fileMetadataRepository = fileMetadataRepository;
        _logger = logger;
    }

    public async Task<Result<SharedKernelError, ImageUploadResult>> UploadImageAsync(
        Stream stream,
        string fileName,
        FileType fileType,
        long maxSizeBytes = 5 * 1024 * 1024,
        SupportedMediaFormat allowedFormats = SupportedMediaFormat.AllImages,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.ValidateImage(stream, fileName, maxSizeBytes, allowedFormats);
        if (!validationResult.IsSuccess)
        {
            return Result<SharedKernelError, ImageUploadResult>.Failure(
                validationResult.Error, validationResult.ErrorMessage);
        }

        var validated = validationResult.Data!;
        var fileId = Guid.CreateVersion7();
        var storageKey = ResolveStorageKey(fileType, fileId);

        var fileMetadata = FileMetadata.CreateAvailable(
            fileId,
            storageKey,
            fileName,
            fileType,
            validated.ContentType,
            validated.SizeBytes);

        await _fileMetadataRepository.AddAsync(fileMetadata, cancellationToken);

        try
        {
            if (stream.CanSeek)
                stream.Position = 0;

            await _storageService.UploadAsync(
                fileId,
                storageKey,
                stream,
                validated.ContentType,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image {FileId} to storage key {StorageKey}", fileId, storageKey);
            return Result<SharedKernelError, ImageUploadResult>.Failure(
                SharedKernelError.StorageUnavailable, "Object storage upload failed. Please try again.");
        }

        var publicUrl = _storageService.GetPublicUrl(storageKey);
        _logger.LogInformation("Image {FileId} uploaded successfully to {StorageKey} for type {FileType}", fileId, storageKey, fileType);

        return Result<SharedKernelError, ImageUploadResult>.Success(
            new ImageUploadResult(fileId, storageKey, publicUrl, validated.ContentType, validated.SizeBytes));
    }

    public async Task<Result<SharedKernelError, ImageUploadResult>> ReplaceImageAsync(
        Stream newImageStream,
        string fileName,
        FileType fileType,
        string? currentPublicUrlOrKey,
        long maxSizeBytes = 5 * 1024 * 1024,
        SupportedMediaFormat allowedFormats = SupportedMediaFormat.AllImages,
        CancellationToken cancellationToken = default)
    {
        var uploadResult = await UploadImageAsync(
            newImageStream, fileName, fileType, maxSizeBytes, allowedFormats, cancellationToken);

        if (!uploadResult.IsSuccess)
            return uploadResult;

        if (!string.IsNullOrWhiteSpace(currentPublicUrlOrKey))
        {
            await DeleteImageAsync(currentPublicUrlOrKey, cancellationToken);
        }

        return uploadResult;
    }

    public async Task DeleteImageAsync(
        string currentPublicUrlOrKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentPublicUrlOrKey))
            return;

        var key = ExtractStorageKey(currentPublicUrlOrKey);
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            var metadata = await _fileMetadataRepository.GetByStorageKeyAsync(key, cancellationToken);
            if (metadata != null && !metadata.IsDeleted)
            {
                metadata.MarkAsDeleted();
                await _fileMetadataRepository.UpdateAsync(metadata, cancellationToken);
            }

            await _storageService.DeleteAsync(key, cancellationToken);
            _logger.LogInformation("Deleted image at storage key {StorageKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image at key {StorageKey}", key);
        }
    }

    private static string ResolveStorageKey(FileType fileType, Guid fileId) => fileType switch
    {
        FileType.ProfilePic => StorageKeyGenerator.ProfilePicture(fileId),
        FileType.ProfileBanner => StorageKeyGenerator.ProfileBanner(fileId),
        FileType.AlbumCover => StorageKeyGenerator.AlbumCover(fileId),
        FileType.PlaylistCover => StorageKeyGenerator.PlaylistCover(fileId),
        _ => throw new ArgumentOutOfRangeException(nameof(fileType), $"Unsupported image file type: {fileType}")
    };

    private static string ExtractStorageKey(string urlOrKey)
    {
        if (string.IsNullOrWhiteSpace(urlOrKey)) return string.Empty;

        const string publicMarker = "public/images/";
        var index = urlOrKey.IndexOf(publicMarker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            return urlOrKey[index..].TrimEnd('/');
        }

        return urlOrKey.TrimStart('/');
    }
}
