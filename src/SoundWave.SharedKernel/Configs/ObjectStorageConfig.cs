namespace SoundWave.SharedKernel.Configs;

/// <summary>
/// Configuration options for S3-compatible object storage providers (RustFS, MinIO, AWS S3).
/// </summary>
public sealed class ObjectStorageConfig
{
    /// <summary>
    /// The configuration section name within application configuration files.
    /// </summary>
    public const string SectionName = "ObjectStorage";

    /// <summary>
    /// The service URL of the object storage server (e.g., http://localhost:9000).
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// The public base URL used to construct publicly accessible direct URLs (e.g., http://localhost:9000/soundwave-storage).
    /// If left empty, falls back to path-style addressing using ServiceUrl and BucketName.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The access key ID or username for authentication with the storage service.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The secret access key or password for authentication with the storage service.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The name of the primary bucket used for storing files.
    /// </summary>
    public string BucketName { get; set; } = "soundwave-storage";

    /// <summary>
    /// The maximum allowable file size in bytes for direct API uploads (default 250 MB).
    /// Uploads exceeding this threshold must use presigned upload URLs.
    /// </summary>
    public long MaxDirectUploadSizeBytes { get; set; } = 250L * 1024 * 1024;

    /// <summary>
    /// When true, requests use path-style addressing (http://s3.host/bucket) required by RustFS and MinIO.
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// When true, automatically verifies and creates the bucket, policies, and lifecycle rules during application startup.
    /// </summary>
    public bool AutoProvision { get; set; } = true;

    /// <summary>
    /// Configuration settings for automated object retention and lifecycle management.
    /// </summary>
    public ObjectStorageLifecycleConfig Lifecycle { get; set; } = new();

    /// <summary>
    /// Event notification configurations on the primary bucket (e.g., RabbitMQ AMQP queue).
    /// </summary>
    public List<ObjectStorageNotificationConfig> Notifications { get; set; } = [];
}

/// <summary>
/// Defines an event notification subscription for the object storage bucket.
/// </summary>
public sealed class ObjectStorageNotificationConfig
{
    /// <summary>Unique identifier for the notification rule.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Queue ARN or destination (e.g., "arn:aws:sqs:soundwave:storage-events" or AMQP queue name).</summary>
    public string Arn { get; set; } = string.Empty;

    /// <summary>Events that trigger this notification (e.g. "s3:ObjectCreated:*", "s3:ObjectRemoved:*").</summary>
    public List<string> Events { get; set; } = ["s3:ObjectCreated:*", "s3:ObjectRemoved:*"];
}

/// <summary>
/// Retention and lifecycle settings for soft-deleted objects in the storage bucket.
/// </summary>
public sealed class ObjectStorageLifecycleConfig
{
    /// <summary>
    /// When true, configures bucket lifecycle policies to automatically purge soft-deleted objects.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Number of days an object marked with the expired tag is retained before permanent deletion by storage.
    /// </summary>
    public int ExpiredRetentionDays { get; set; } = 30;

    /// <summary>
    /// The tag key applied to objects to denote their status (e.g., "status").
    /// </summary>
    public string StatusTagKey { get; set; } = "status";

    /// <summary>
    /// The tag value applied to denote an expired/soft-deleted object (e.g., "expired").
    /// </summary>
    public string ExpiredTagValue { get; set; } = "expired";
}
