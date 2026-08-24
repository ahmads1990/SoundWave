# SoundWave Database Schema (v5)
> **Architecture:** Modular Monolith · **DBMS:** MSSQL · **ID Strategy:** GUID v7 (sequential)

---

## 🔐 Identity Module
**Schema:** `Identity`  
**Purpose:** Users, auth tokens, and roles. All other modules reference `UserId` by value only (no cross-module FK constraints).

### `Identity.Users`
Core user account.
- `Id` (PK, UNIQUEIDENTIFIER)
- `Email` (NVARCHAR(256), UNIQUE)
- `PasswordHash` (NVARCHAR(512), BCrypt)
- `DisplayName` (NVARCHAR(100))
- `Role` (TINYINT: 0=Listener, 1=Artist, 2=Admin)
- `IsEmailVerified` (BIT)
- `IsLocked` (BIT)
- `FollowerCount` / `FollowingCount` (INT, Denormalized)
- `ProfilePicUrl` (NVARCHAR(500), optional - stored on UserProfiles)
- `CoverImageUrl` (NVARCHAR(500), optional - stored on UserProfiles, added in Phase 1.9)
- *BaseEntity columns included*

### `Identity.RefreshTokens`
Hashed refresh tokens for session management.
- `Id` (PK, UNIQUEIDENTIFIER)
- `UserId` (FK → Identity.Users)
- `TokenHash` (NVARCHAR(512), SHA-256)
- `ExpiresAt` / `RevokedAt` (DATETIME2)

---

## 🎵 Catalog Module
**Schema:** `Catalog`  
**Purpose:** The core music content module.

### `Catalog.ArtistAccountApprovals`
Application lifecycle table. Admin reviews these; an `Artist` row is only created on approval.
- `Id` (PK, UNIQUEIDENTIFIER)
- `UserId` (Ref ⟶ Identity.Users, UNIQUE — one active application per user)
- `StageName` (NVARCHAR(100))
- `Bio` (NVARCHAR(1000), optional)
- `Status` (TINYINT: 0=Pending, 1=Approved, 2=Rejected)
- `RejectionReason` (NVARCHAR(500), nullable)
- `ReviewedBy` (Ref ⟶ Identity.Users, nullable — admin UserId)
- `ReviewedAt` (DATETIME2, nullable)
- *BaseEntity columns included*

### `Catalog.Artists`
- `Id` (PK, UNIQUEIDENTIFIER)
- `UserId` (Ref ⟶ Identity.Users, UNIQUE)
- `StageName` (NVARCHAR(100))
- `Bio` (NVARCHAR(1000), optional)
- `FollowerCount` / `MonthlyListeners` / `TotalStreams` (Denormalized)
- `ApprovedAt` (DATETIME2, nullable)
- *No `IsApproved` — if this row exists, the artist is approved. Approval state lives in `ArtistAccountApprovals`.*

### `Catalog.Albums`
- `Id` (PK, UNIQUEIDENTIFIER)
- `Title` (NVARCHAR(200))
- `AlbumType` (TINYINT: 0=Album, 1=EP, 2=Single)
- `IsPublished` (BIT)
- `CoverImageUrl` (NVARCHAR(500), optional)
- `Description` (NVARCHAR(1000), optional)
- `ReleaseDate` (DATETIME2, nullable)
- `TrackCount` (INT)

### `Catalog.Tracks`
- `Id` (PK, UNIQUEIDENTIFIER)
- `AlbumId` (FK → Catalog.Albums)
- `Title` (NVARCHAR(200))
- `TrackNumber` (INT)
- `DurationSeconds` (INT)
- `PlayCount` (BIGINT, Redis-flushed)
- `LikeCount` (INT)

### `Catalog.TrackFiles`
Processing/HLS metadata (1:1 with Tracks).
- `TrackId` (FK → Catalog.Tracks, UNIQUE)
- `Status` (TINYINT: 0=Pending, 1=Processing, 2=Ready, 3=Failed)
- `HlsPlaylistPath` / `PreviewPlaylistPath` (NVARCHAR(500))
- `RawFilePath` (NVARCHAR(500), optional)
- `FileSizeBytes` (BIGINT)

### `Catalog.Genres`
- `Id` (INT, Identity)
- `Name` (NVARCHAR(50))
- `Type` (TINYINT: 0=Genre, 1=Mood)

**Junction Tables:**
- `Catalog.AlbumArtists` (Album ↔ Artist)
- `Catalog.TrackArtists` (Track ↔ Artist)
- `Catalog.TrackGenres` (Track ↔ Genre)
- `Catalog.AlbumGenres` (Album ↔ Genre)

---

## ▶️ Streaming Module
**Schema:** `Streaming`  
**Purpose:** Playback events and session persistence.

### `Streaming.PlayHistory`
Append-only log of play events (high volume).
- `Id` (PK, BIGINT)
- `UserId` (Ref ⟶ Identity.Users)
- `TrackId` (Ref ⟶ Catalog.Tracks)
- `PlayedAt` (DATETIME2)
- `WasSkipped` (BIT, Qdrant negative signal)

### `Streaming.QueueSessions`
Durable backup of the user's active queue.
- `UserId` (Ref ⟶ Identity.Users, UNIQUE)
- `CurrentTrackId` (Ref ⟶ Catalog.Tracks)
- `QueueJson` (NVARCHAR(MAX))

### `Streaming.SearchHistory`
- `UserId` (Ref ⟶ Identity.Users)
- `Query` (NVARCHAR(200))

---

## 📋 Playlist Module
**Schema:** `Playlist`  
**Purpose:** Playlists and liked content.

### `Playlist.Playlists`
- `Id` (PK, UNIQUEIDENTIFIER)
- `OwnerId` (Ref ⟶ Identity.Users)
- `Visibility` (TINYINT: 0=Private, 1=Public, 2=Collaborative)
- `IsSystem` (BIT, e.g. "Liked Songs")

### `Playlist.PlaylistTracks`
- `PlaylistId` (FK → Playlist.Playlists)
- `TrackId` (Ref ⟶ Catalog.Tracks)
- `Position` (INT)

**Junctions & Likes:**
- `Playlist.PlaylistCollaborators`
- `Playlist.LikedTracks`
- `Playlist.LikedAlbums`
- `Playlist.LikedPlaylists`

---

## 👥 Social Module
**Schema:** `Social`  
**Purpose:** Following, posts, and notifications.

### `Social.UserFollows` / `Social.ArtistFollows`
Composite PKs tracking followers.

### `Social.ArtistPosts`
Artist updates shown in home feed.
- `ArtistId` (Ref ⟶ Catalog.Artists)
- `Body` (NVARCHAR(2000))

### `Social.Notifications`
- `UserId` (Ref ⟶ Identity.Users)
- `Type` (TINYINT: 0=ArtistApproved, 1=NewFollower, etc.)
- `ReferenceId` (Ref ⟶ Any module entity)

---

## 📊 Analytics Module
**Schema:** `Analytics`  
**Purpose:** Read-only aggregate tables populated nightly.

- `Analytics.DailyTrackStats`
- `Analytics.DailyArtistStats`
- `Analytics.MonthlyUserStats`
- `Analytics.PlatformDailyStats` (DAU, New Regs, total streams)

---

## ⚙️ Infra Module
**Schema:** `Infra`  
**Purpose:** Shared cross-cutting concerns.

### `Infra.OutboxMessages`
Transactional outbox for RabbitMQ publishing.
- `Type` / `RoutingKey` / `Payload` (NVARCHAR)
- `ProcessedAt` / `RetryCount`

### `Infra.AuditLogs`
Admin action history.
- `ActorUserId` (Ref ⟶ Identity.Users)
- `Action` / `OldValues` / `NewValues` (JSON snapshots)

---

## 💡 Key Architectural Rules
1. **Intra-module FKs (✦):** Real database constraints (e.g., `Catalog.Tracks` → `Catalog.Albums`).
2. **Cross-module Refs (✧):** Stored as plain `UNIQUEIDENTIFIER` or `INT` with NO database-level FK constraint. Application logic enforces consistency (e.g., `Playlist.PlaylistTracks` → `Catalog.Tracks`).
3. **BaseEntity:** Every business table includes `CreatedBy`, `CreatedDate`, `UpdatedBy`, `UpdatedDate`, and `IsDeleted` (Soft Delete).
4. **ID Strategy:** GUID v7 is preferred for sequential performance in MSSQL.
