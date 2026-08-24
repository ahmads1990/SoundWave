# SoundWave — DB Schema Quick Reference

## 7 Schemas, 29 Tables Total

### Identity (2 tables)
- `Identity.Users` — Core user (Email, PasswordHash, DisplayName, Role, IsLocked, FollowerCount)
- `Identity.RefreshTokens` — Hashed refresh tokens with rotation chain (TokenHash, ExpiresAt, RevokedAt)
- Redis: login_fails:{email}, blacklist:{jti}, email_verify:{userId}, pwd_reset:{userId}

### Catalog (9 tables)
- `Catalog.Artists` — Artist profile (UserId→Identity cross-module, StageName, Bio, IsApproved, FollowerCount, MonthlyListeners)
- `Catalog.Albums` — Albums/EPs/Singles (Title, CoverArt, ReleaseDate, AlbumType, IsPublished)
- `Catalog.Tracks` — Track metadata (AlbumId, Title, DurationMs, IsExplicit, PlayCount, LikeCount)
- `Catalog.TrackFiles` — Upload + HLS state (RawFilePath, HlsPlaylistPath, PreviewPlaylistPath, Status enum)
- `Catalog.Genres` — Genre/Mood tags (INT PK, Name, Slug, Type)
- `Catalog.AlbumArtists` — M:M junction (composite PK, IsPrimary)
- `Catalog.TrackArtists` — M:M junction (composite PK, Role: Primary/Featured/Remixer)
- `Catalog.TrackGenres` — M:M junction
- `Catalog.AlbumGenres` — M:M junction

### Streaming (3 tables)
- `Streaming.PlayHistory` — Append-only play log (BIGINT PK, UserId, TrackId, PlayedAt, DurationPlayedMs, WasSkipped)
- `Streaming.QueueSessions` — One per user (QueueJson, ShuffleEnabled, RepeatMode)
- `Streaming.SearchHistory` — Recent searches (max 50/user)
- Redis: play_count:{trackId}, playback_pos:{userId}:{trackId}, queue:{userId}

### Playlist (6 tables)
- `Playlist.Playlists` — User/system playlists (OwnerId, Title, Visibility, IsSystem, TrackCount)
- `Playlist.PlaylistTracks` — Ordered tracks (Position, AddedByUserId)
- `Playlist.PlaylistCollaborators` — Collab editors (PermissionLevel)
- `Playlist.LikedTracks` — User track likes (composite PK)
- `Playlist.LikedAlbums` — User album saves (composite PK)
- `Playlist.LikedPlaylists` — Playlist follows (composite PK)

### Social (4 tables)
- `Social.UserFollows` — User→User (FollowerId, FolloweeId, CHECK: not self)
- `Social.ArtistFollows` — User→Artist (UserId, ArtistId)
- `Social.ArtistPosts` — Artist feed posts (Body, ImageUrl, LinkedTrackId, LinkedAlbumId)
- `Social.Notifications` — Bell notifications (Type, Title, Body, ReferenceType/Id, IsRead)

### Analytics (4 tables)
- `Analytics.DailyTrackStats` — Per track/day (StreamCount, UniqueListeners, SkipCount)
- `Analytics.DailyArtistStats` — Per artist/day (StreamCount, FollowerGained/Lost)
- `Analytics.MonthlyUserStats` — Per user/month (TotalListenMs, TopTrackId, TopArtistId)
- `Analytics.PlatformDailyStats` — Platform-wide/day (DAU, TotalStreams, NewRegistrations)

### Infra (2 tables)
- `Infra.OutboxMessages` — Transactional outbox (Type, RoutingKey, Payload, ProcessedAt, RetryCount)
- `Infra.AuditLogs` — Admin audit trail (BIGINT PK, Action, OldValues/NewValues JSON)

## FK Rules
- **Intra-module**: Real FK constraints (✦)
- **Cross-module**: Plain GUID, no FK constraint (✧), app enforces
- **BaseEntity on all tables**: CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, IsDeleted
