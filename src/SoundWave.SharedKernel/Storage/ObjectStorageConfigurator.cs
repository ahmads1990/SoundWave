using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoundWave.SharedKernel.Configs;

namespace SoundWave.SharedKernel.Storage;

/// <summary>
/// Configures S3 object storage buckets, event notifications, lifecycle policies, and public access rules as a startup task.
/// </summary>
public sealed class ObjectStorageConfigurator : IHostedService
{
    private readonly ObjectStorageConfig _config;
    private readonly ILogger<ObjectStorageConfigurator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectStorageConfigurator"/> class.
    /// </summary>
    /// <param name="config">The object storage configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public ObjectStorageConfigurator(
        IOptions<ObjectStorageConfig> config,
        ILogger<ObjectStorageConfigurator> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.AutoProvision || string.IsNullOrWhiteSpace(_config.ServiceUrl) || string.IsNullOrWhiteSpace(_config.BucketName))
        {
            return;
        }

        try
        {
            var credentials = new BasicAWSCredentials(_config.Username, _config.Password);
            var s3Config = new AmazonS3Config
            {
                ServiceURL = _config.ServiceUrl,
                ForcePathStyle = _config.ForcePathStyle,
                UseHttp = !_config.ServiceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            };

            using var client = new AmazonS3Client(credentials, s3Config);

            // 1. Ensure Bucket Exists (Idempotent)
            try
            {
                var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(client, _config.BucketName);
                if (!bucketExists)
                {
                    await client.PutBucketAsync(_config.BucketName, cancellationToken);
                    _logger.LogInformation("Bucket '{BucketName}' created in object storage at {Url}.", _config.BucketName, _config.ServiceUrl);
                }
                else
                {
                    _logger.LogDebug("Bucket '{BucketName}' already exists.", _config.BucketName);
                }
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou" ||
                                               ex.ErrorCode == "BucketAlreadyExists" ||
                                               ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogDebug("Bucket '{BucketName}' already exists.", _config.BucketName);
            }

            // 2. Configure Event Notifications (e.g. RabbitMQ AMQP queue)
            if (_config.Notifications.Count > 0)
            {
                try
                {
                    var notificationRequest = new PutBucketNotificationRequest
                    {
                        BucketName = _config.BucketName,
                        QueueConfigurations = _config.Notifications.Select(n => new QueueConfiguration
                        {
                            Id = n.Id,
                            Queue = n.Arn,
                            Events = n.Events.Select(e => EventType.FindValue(e)).ToList()
                        }).ToList()
                    };

                    await client.PutBucketNotificationAsync(notificationRequest, cancellationToken);
                    _logger.LogInformation("Configured {Count} notification(s) on bucket '{BucketName}'.", _config.Notifications.Count, _config.BucketName);
                }
                catch (AmazonS3Exception ex)
                {
                    _logger.LogWarning(ex, "Could not apply notification configuration on bucket '{BucketName}'. Continuing storage setup.", _config.BucketName);
                }
            }

            // 3. Configure Lifecycle Rules for soft-deleted items
            if (_config.Lifecycle.Enabled)
            {
                try
                {
                    var lifecycleRequest = new PutLifecycleConfigurationRequest
                    {
                        BucketName = _config.BucketName,
                        Configuration = new LifecycleConfiguration
                        {
                            Rules =
                            [
                                new LifecycleRule
                                {
                                    Id = "PurgeExpiredTaggedObjectsRule",
                                    Status = LifecycleRuleStatus.Enabled,
                                    Expiration = new LifecycleRuleExpiration { Days = _config.Lifecycle.ExpiredRetentionDays },
                                    Filter = new LifecycleFilter
                                    {
                                        LifecycleFilterPredicate = new LifecycleTagPredicate
                                        {
                                            Tag = new Tag
                                            {
                                                Key = _config.Lifecycle.StatusTagKey,
                                                Value = _config.Lifecycle.ExpiredTagValue
                                            }
                                        }
                                    }
                                }
                            ]
                        }
                    };

                    await client.PutLifecycleConfigurationAsync(lifecycleRequest, cancellationToken);
                    _logger.LogInformation("Applied lifecycle rule to bucket '{BucketName}' (purge tagged '{TagKey}={TagValue}' after {Days} days).", _config.BucketName, _config.Lifecycle.StatusTagKey, _config.Lifecycle.ExpiredTagValue, _config.Lifecycle.ExpiredRetentionDays);
                }
                catch (AmazonS3Exception ex)
                {
                    _logger.LogWarning(ex, "Could not apply lifecycle configuration on bucket '{BucketName}'. Continuing storage setup.", _config.BucketName);
                }
            }

            // 4. Configure Public Read Policy for objects with 'public/*' prefix
            try
            {
                var publicPolicy = $$"""
                {
                    "Version": "2012-10-17",
                    "Statement": [
                        {
                            "Sid": "AllowPublicReadForPublicPrefix",
                            "Effect": "Allow",
                            "Principal": "*",
                            "Action": ["s3:GetObject"],
                            "Resource": [
                                "arn:aws:s3:::{{_config.BucketName}}/public/*"
                            ]
                        }
                    ]
                }
                """;

                await client.PutBucketPolicyAsync(new PutBucketPolicyRequest
                {
                    BucketName = _config.BucketName,
                    Policy = publicPolicy
                }, cancellationToken);

                _logger.LogInformation("Applied public read policy to bucket '{BucketName}' for 'public/*' prefix.", _config.BucketName);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogWarning(ex, "Could not apply public policy on bucket '{BucketName}'. Continuing storage setup.", _config.BucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-provision object storage bucket '{BucketName}' at {Url}. Verify storage connectivity.", _config.BucketName, _config.ServiceUrl);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
