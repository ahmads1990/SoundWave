using System.Text.Json.Serialization;

namespace SoundWave.SharedKernel.Storage.Events;

/// <summary>
/// Object storage bucket notification model deserialized from message queue events
/// published by object storage providers (RustFS, MinIO, S3).
/// </summary>
public class BucketNotification
{
    /// <summary>
    /// Collection of event records included in the notification payload.
    /// </summary>
    [JsonPropertyName("Records")]
    public List<BucketNotificationRecord> Records { get; set; } = [];
}

/// <summary>
/// Represents an individual S3 event record.
/// </summary>
public class BucketNotificationRecord
{
    /// <summary>Event schema version.</summary>
    [JsonPropertyName("eventVersion")]
    public string? EventVersion { get; set; }

    /// <summary>Event source system (e.g. "aws:s3" or "rustfs:s3").</summary>
    [JsonPropertyName("eventSource")]
    public string? EventSource { get; set; }

    /// <summary>Timestamp when the event occurred.</summary>
    [JsonPropertyName("eventTime")]
    public DateTime? EventTime { get; set; }

    /// <summary>Name of the triggered event (e.g. "s3:ObjectCreated:Put", "s3:ObjectRemoved:Delete").</summary>
    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    /// <summary>Storage metadata detailing bucket and object information.</summary>
    [JsonPropertyName("s3")]
    public BucketNotificationEntity Storage { get; set; } = new();
}

/// <summary>
/// S3-specific details contained within a bucket notification record.
/// </summary>
public class BucketNotificationEntity
{
    /// <summary>S3 notification schema version.</summary>
    [JsonPropertyName("s3SchemaVersion")]
    public string? SchemaVersion { get; set; }

    /// <summary>Identifier of the notification configuration rule.</summary>
    [JsonPropertyName("configurationId")]
    public string? ConfigurationId { get; set; }

    /// <summary>Bucket details where the event occurred.</summary>
    [JsonPropertyName("bucket")]
    public BucketNotificationBucket Bucket { get; set; } = new();

    /// <summary>Object details for the subject of the event.</summary>
    [JsonPropertyName("object")]
    public BucketNotificationObject Object { get; set; } = new();
}

/// <summary>
/// Bucket details within an S3 notification.
/// </summary>
public class BucketNotificationBucket
{
    /// <summary>The name of the bucket.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The Amazon Resource Name (ARN) of the bucket.</summary>
    [JsonPropertyName("arn")]
    public string? Arn { get; set; }
}

/// <summary>
/// Object details within an S3 notification.
/// </summary>
public class BucketNotificationObject
{
    /// <summary>The key path of the object in the bucket.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>The size of the object in bytes.</summary>
    [JsonPropertyName("size")]
    public long? Size { get; set; }

    /// <summary>The entity tag / checksum of the object.</summary>
    [JsonPropertyName("eTag")]
    public string? ETag { get; set; }

    /// <summary>A string representation of the sequence of the object modification.</summary>
    [JsonPropertyName("sequencer")]
    public string? Sequencer { get; set; }

    /// <summary>Optional custom user metadata headers (<c>x-amz-meta-*</c>).</summary>
    [JsonPropertyName("userMetadata")]
    public Dictionary<string, string>? UserMetadata { get; set; }
}
