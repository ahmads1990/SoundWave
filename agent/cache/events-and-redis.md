# SoundWave — RabbitMQ Events & Redis Keys Cache

## RabbitMQ Topology
- Exchange: `soundwave.events` (topic exchange)
- Routing pattern: `catalog.<entity>.<verb>` | `streaming.<verb>` | `identity.<verb>`
- Every queue has a `.dlq` sibling
- Raw RabbitMQ.Client — no MassTransit

## Events
| Event | Routing Key | Producer | Consumers |
|-------|-------------|----------|-----------|
| TrackUploaded | catalog.track.uploaded | UploadTrackCommandHandler (Outbox) | catalog.processing (FFmpeg), catalog.search (ES), catalog.embeddings (Qdrant) |
| PlaybackRecorded | streaming.playback.recorded | RecordPlayCommandHandler (Outbox) | analytics.playback (stats), recommendations.history (vectors) |
| UserRegistered | identity.user.registered | RegisterCommandHandler (Outbox) | identity.email (verify), identity.setup (Liked Songs playlist) |
| ArtistApproved | identity.artist.approved | ApproveArtistCommandHandler (Outbox) | identity.email (notify), catalog.artist.index (ES) |

## Redis Keys
| Key Pattern | Purpose | TTL |
|-------------|---------|-----|
| play_count:{trackId} | INCR per play, flushed to DB every 5 min | None (flushed) |
| playback_pos:{userId}:{trackId} | Resume position | 30 days |
| queue:{userId} | Active queue JSON | Session-based |
| blacklist:{jti} | Revoked JWT access tokens | Remaining JWT lifetime |
| email_verify:{userId} | Email verification token | 24h |
| pwd_reset:{userId} | Password reset token | 1h |
| login_fails:{email} | Failed login counter, lock after 5 | 15 min |
| ratelimit:{ip} | Sliding window rate limiter | Per window |
| autocomplete:{prefix} | Search autocomplete cache (Phase 5) | 5 min |

## Background Workers
- OutboxProcessorWorker: polls OutboxMessages, publishes to RabbitMQ (every 5s)
- PlayCountFlushWorker: Redis → DB play counts (every 5 min)
- AnalyticsRollupWorker: nightly PlayHistory → stats aggregation
- StuckTrackDetector: marks Processing > 30 min as Failed
