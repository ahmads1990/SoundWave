using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoundWave.SharedKernel.Configs;

namespace SoundWave.SharedKernel.Storage;

/// <summary>
/// S3-compatible implementation of <see cref="IObjectStorageService"/> supporting RustFS, MinIO, and AWS S3.
/// </summary>
public class ObjectStorageService : IObjectStorageService
{
    private readonly IAmazonS3 _storageClient;
    private readonly ObjectStorageConfig _config;
    private readonly ILogger<ObjectStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectStorageService"/> class.
    /// </summary>
    /// <param name="storageClient">The AWS S3 client instance.</param>
    /// <param name="config">The object storage configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public ObjectStorageService(
        IAmazonS3 storageClient,
        IOptions<ObjectStorageConfig> config,
        ILogger<ObjectStorageService> logger)
    {
        _storageClient = storageClient;
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> GenerateUploadUrlAsync(
        Guid fileId,
        string key,
        TimeSpan expiry,
        string? contentType = null,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating presigned upload URL for object '{ObjectKey}' (FileId: {FileId}, Expiry: {Expiry})", key, fileId, expiry);

        var preSignedUrlRequest = new GetPreSignedUrlRequest
        {
            BucketName = _config.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            preSignedUrlRequest.ContentType = contentType;
        }

        // Attach mandatory file-id metadata to S3 object
        preSignedUrlRequest.Metadata["file-id"] = fileId.ToString();

        if (metadata != null)
        {
            foreach (var (metaKey, metaValue) in metadata)
            {
                preSignedUrlRequest.Metadata[metaKey] = metaValue;
            }
        }

        var url = _storageClient.GetPreSignedURL(preSignedUrlRequest);
        return Task.FromResult(url);
    }

    /// <inheritdoc />
    public Task<string> GenerateDownloadUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating presigned download URL for object '{ObjectKey}' (Expiry: {Expiry})", key, expiry);

        var preSignedUrlRequest = new GetPreSignedUrlRequest
        {
            BucketName = _config.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        var url = _storageClient.GetPreSignedURL(preSignedUrlRequest);
        return Task.FromResult(url);
    }

    /// <inheritdoc />
    public async Task UploadAsync(
        Guid fileId,
        string key,
        Stream content,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (content.CanSeek && content.Length > _config.MaxDirectUploadSizeBytes)
        {
            var sizeMb = content.Length / (1024 * 1024);
            var maxMb = _config.MaxDirectUploadSizeBytes / (1024 * 1024);
            _logger.LogWarning("Direct upload rejected for '{ObjectKey}': size {SizeMb}MB exceeds maximum limit of {MaxMb}MB", key, sizeMb, maxMb);
            throw new InvalidOperationException($"Direct upload size ({sizeMb}MB) exceeds the maximum allowed limit of {maxMb}MB. Use presigned URL upload instead.");
        }

        _logger.LogInformation("Uploading object '{ObjectKey}' (FileId: {FileId}, ContentType: {ContentType}) to bucket '{BucketName}'", key, fileId, contentType, _config.BucketName);

        var putRequest = new PutObjectRequest
        {
            BucketName = _config.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        putRequest.Metadata["file-id"] = fileId.ToString();

        if (metadata != null)
        {
            foreach (var (metaKey, metaValue) in metadata)
            {
                putRequest.Metadata[metaKey] = metaValue;
            }
        }

        await _storageClient.PutObjectAsync(putRequest, cancellationToken);
        _logger.LogInformation("Successfully uploaded object '{ObjectKey}' to bucket '{BucketName}'", key, _config.BucketName);
    }

    /// <inheritdoc />
    public async Task UploadAsync(
        Guid fileId,
        string key,
        byte[] content,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);
        await UploadAsync(fileId, key, stream, contentType, metadata, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _storageClient.GetObjectAsync(_config.BucketName, key, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_config.Lifecycle.Enabled)
        {
            _logger.LogInformation("Soft-deleting object '{ObjectKey}' in bucket '{BucketName}' with tag '{TagKey}={TagValue}'", key, _config.BucketName, _config.Lifecycle.StatusTagKey, _config.Lifecycle.ExpiredTagValue);

            var taggingRequest = new PutObjectTaggingRequest
            {
                BucketName = _config.BucketName,
                Key = key,
                Tagging = new Tagging
                {
                    TagSet =
                    [
                        new Tag
                        {
                            Key = _config.Lifecycle.StatusTagKey,
                            Value = _config.Lifecycle.ExpiredTagValue
                        }
                    ]
                }
            };

            await _storageClient.PutObjectTaggingAsync(taggingRequest, cancellationToken);
            _logger.LogInformation("Successfully tagged object '{ObjectKey}' as expired for lifecycle purge.", key);
        }
        else
        {
            _logger.LogInformation("Physically deleting object '{ObjectKey}' from bucket '{BucketName}'", key, _config.BucketName);
            await _storageClient.DeleteObjectAsync(_config.BucketName, key, cancellationToken);
            _logger.LogInformation("Successfully deleted object '{ObjectKey}' from bucket '{BucketName}'", key, _config.BucketName);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _storageClient.GetObjectMetadataAsync(_config.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string GetPublicUrl(string key)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(_config.PublicBaseUrl)
            ? _config.PublicBaseUrl.TrimEnd('/')
            : $"{_config.ServiceUrl.TrimEnd('/')}/{_config.BucketName}";

        return $"{baseUrl}/{key.TrimStart('/')}";
    }

    /// <inheritdoc />
    public Task<string> GeneratePublicUrlAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(GetPublicUrl(key));
}
