import { useState } from "react";

const BASE_ENTITY_NOTE = "BaseEntity";
const BASE_COLS = [
  { name: "CreatedBy", type: "UNIQUEIDENTIFIER", constraints: "NOT NULL", note: BASE_ENTITY_NOTE },
  { name: "CreatedDate", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: BASE_ENTITY_NOTE },
  { name: "UpdatedBy", type: "UNIQUEIDENTIFIER", constraints: "NULL", note: BASE_ENTITY_NOTE },
  { name: "UpdatedDate", type: "DATETIME2", constraints: "NULL", note: BASE_ENTITY_NOTE },
  { name: "IsDeleted", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "Soft delete — EF global filter" },
];

// FK rule: INTRA = real FK constraint, CROSS = ID only, no constraint
const FK = {
  intra: (to) => `NOT NULL → ${to} ✦`,      // same schema
  cross: (to) => `NOT NULL ⟶ ${to} ✧`,     // cross-module, no FK constraint
  intraOpt: (to) => `NULL → ${to} ✦`,
  crossOpt: (to) => `NULL ⟶ ${to} ✧`,
};

const modules = [
  // ─────────────────────────────────────────────────────────────────────────
  {
    id: "identity",
    schema: "Identity",
    label: "Identity",
    color: "#E91E8C",
    icon: "🔐",
    desc: "Users, auth tokens, roles. All other modules reference UserId by value only — no cross-module FK.",
    tables: [
      {
        name: "Users",
        fullName: "Identity.Users",
        icon: "👤",
        purpose: "Core user account shared by all roles. Role column = 0 Listener / 1 Artist / 2 Admin.",
        redisNote: "login_fails:{email} → INCR, TTL 15 min (lockout counter)\nblacklist:{jti} → TTL = remaining JWT lifetime (access token revocation)",
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "Email", type: "NVARCHAR(256)", constraints: "NOT NULL UNIQUE", note: "" },
          { name: "PasswordHash", type: "NVARCHAR(512)", constraints: "NOT NULL", note: "BCrypt" },
          { name: "DisplayName", type: "NVARCHAR(100)", constraints: "NOT NULL", note: "" },
          { name: "AvatarUrl", type: "NVARCHAR(500)", constraints: "NULL", note: "" },
          { name: "Role", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "0=Listener 1=Artist 2=Admin" },
          { name: "IsEmailVerified", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "Flipped by Redis token consumption" },
          { name: "IsLocked", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "Set after 5 failed logins" },
          { name: "FollowerCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "Denormalized" },
          { name: "FollowingCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "Denormalized" },
          ...BASE_COLS,
        ],
        indexes: ["UX_Identity_Users_Email", "IX_Identity_Users_Role"],
        fkNote: "This is the root identity table. All other modules store UserId as a plain UNIQUEIDENTIFIER — no FK constraint pointing here from other schemas.",
      },
      {
        name: "RefreshTokens",
        fullName: "Identity.RefreshTokens",
        icon: "🔑",
        purpose: "Durable hashed refresh tokens. Rotated on every use. Revoked on logout.",
        redisNote: "email_verify:{userId} → TTL 24h (email verification token)\npwd_reset:{userId} → TTL 1h (password reset token)\nNote: these short-lived tokens live ONLY in Redis — no DB table needed.",
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Identity.Users"), note: "" },
          { name: "TokenHash", type: "NVARCHAR(512)", constraints: "NOT NULL UNIQUE", note: "SHA-256 of raw token" },
          { name: "ExpiresAt", type: "DATETIME2", constraints: "NOT NULL", note: "7 days from issue" },
          { name: "RevokedAt", type: "DATETIME2", constraints: "NULL", note: "Set on rotation or logout" },
          { name: "DeviceInfo", type: "NVARCHAR(200)", constraints: "NULL", note: "" },
          { name: "ReplacedByTokenId", type: "UNIQUEIDENTIFIER", constraints: FK.intraOpt("Identity.RefreshTokens"), note: "Token chain audit" },
          ...BASE_COLS,
        ],
        indexes: ["IX_Identity_RefreshTokens_UserId", "UX_Identity_RefreshTokens_TokenHash"],
        fkNote: null,
      },
    ],
  },

  // ─────────────────────────────────────────────────────────────────────────
  {
    id: "catalog",
    schema: "Catalog",
    label: "Catalog",
    color: "#1DB954",
    icon: "🎵",
    desc: "Artists, Albums, Tracks, TrackFiles, Genres. The core music content module. References Identity.UserId cross-module.",
    tables: [
      {
        name: "Artists",
        fullName: "Catalog.Artists",
        icon: "🎤",
        purpose: "Artist profile. 1:1 optional link to Identity.Users via UserId (no FK — cross-module).",
        redisNote: null,
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users") + " UNIQUE", note: "Cross-module — ID only, no FK constraint" },
          { name: "StageName", type: "NVARCHAR(100)", constraints: "NOT NULL", note: "" },
          { name: "Bio", type: "NVARCHAR(2000)", constraints: "NULL", note: "" },
          { name: "ProfileImageUrl", type: "NVARCHAR(500)", constraints: "NULL", note: "" },
          { name: "IsVerified", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "IsApproved", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "Admin gate before uploads" },
          { name: "ApprovedAt", type: "DATETIME2", constraints: "NULL", note: "" },
          { name: "ApprovedByUserId", type: "UNIQUEIDENTIFIER", constraints: FK.crossOpt("Identity.Users"), note: "Cross-module" },
          { name: "FollowerCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "Denormalized" },
          { name: "MonthlyListeners", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "Updated by Analytics Worker" },
          { name: "TotalStreams", type: "BIGINT", constraints: "NOT NULL DEFAULT 0", note: "Updated by Analytics Worker" },
          ...BASE_COLS,
        ],
        indexes: ["UX_Catalog_Artists_UserId", "IX_Catalog_Artists_IsApproved", "IX_Catalog_Artists_StageName"],
        fkNote: "UserId is cross-module (Identity). Stored as plain ID. No FK constraint.",
      },
      {
        name: "Albums",
        fullName: "Catalog.Albums",
        icon: "💿",
        purpose: "Music album, EP, or single. Owned by one or more artists via AlbumArtists junction.",
        redisNote: null,
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "Title", type: "NVARCHAR(200)", constraints: "NOT NULL", note: "" },
          { name: "CoverArtUrl", type: "NVARCHAR(500)", constraints: "NULL", note: "" },
          { name: "ReleaseDate", type: "DATE", constraints: "NULL", note: "NULL = unreleased / scheduled" },
          { name: "AlbumType", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "0=Album 1=EP 2=Single" },
          { name: "Label", type: "NVARCHAR(100)", constraints: "NULL", note: "" },
          { name: "Description", type: "NVARCHAR(1000)", constraints: "NULL", note: "" },
          { name: "IsPublished", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "TotalTracks", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "Denormalized" },
          { name: "TotalDurationMs", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "Denormalized" },
          ...BASE_COLS,
        ],
        indexes: ["IX_Catalog_Albums_ReleaseDate", "IX_Catalog_Albums_AlbumType", "IX_Catalog_Albums_IsPublished"],
        fkNote: null,
      },
      {
        name: "Tracks",
        fullName: "Catalog.Tracks",
        icon: "🎵",
        purpose: "Pure music metadata. Processing/HLS concerns live in TrackFiles (1:1).",
        redisNote: "play_count:{trackId} → Redis INCR per play, flushed to Tracks.PlayCount every 5 min by Worker",
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "AlbumId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Catalog.Albums"), note: "" },
          { name: "Title", type: "NVARCHAR(200)", constraints: "NOT NULL", note: "" },
          { name: "DurationMs", type: "INT", constraints: "NULL", note: "NULL until TrackFiles.Status = Ready" },
          { name: "TrackNumber", type: "TINYINT", constraints: "NULL", note: "" },
          { name: "DiscNumber", type: "TINYINT", constraints: "NOT NULL DEFAULT 1", note: "" },
          { name: "Isrc", type: "VARCHAR(12)", constraints: "NULL UNIQUE", note: "" },
          { name: "IsExplicit", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "IsPublished", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "Only Published + Ready tracks streamable" },
          { name: "PlayCount", type: "BIGINT", constraints: "NOT NULL DEFAULT 0", note: "Redis-flushed every 5 min" },
          { name: "LikeCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "Denormalized from Library.LikedTracks" },
          ...BASE_COLS,
        ],
        indexes: ["IX_Catalog_Tracks_AlbumId", "UX_Catalog_Tracks_Isrc", "IX_Catalog_Tracks_PlayCount DESC", "IX_Catalog_Tracks_IsPublished"],
        fkNote: null,
      },
      {
        name: "TrackFiles",
        fullName: "Catalog.TrackFiles",
        icon: "📁",
        purpose: "Upload + HLS processing state for a track. 1:1 with Tracks. Only joined when streaming or checking upload status.",
        redisNote: null,
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "TrackId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Catalog.Tracks") + " UNIQUE", note: "1:1" },
          { name: "RawFilePath", type: "NVARCHAR(500)", constraints: "NULL", note: "Cleared after processing (configurable)" },
          { name: "RawFileFormat", type: "VARCHAR(10)", constraints: "NULL", note: "mp3/flac/aac/wav" },
          { name: "RawFileSizeBytes", type: "BIGINT", constraints: "NULL", note: "" },
          { name: "HlsPlaylistPath", type: "NVARCHAR(500)", constraints: "NULL", note: "Set when Status = Ready" },
          { name: "PreviewPlaylistPath", type: "NVARCHAR(500)", constraints: "NULL", note: "30s preview .m3u8" },
          { name: "Status", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "0=Pending 1=Processing 2=Ready 3=Failed" },
          { name: "FailureReason", type: "NVARCHAR(500)", constraints: "NULL", note: "FFmpeg stderr on failure" },
          { name: "RetryCount", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "ProcessingStartedAt", type: "DATETIME2", constraints: "NULL", note: "Used by stuck-track detector" },
          { name: "ProcessingCompletedAt", type: "DATETIME2", constraints: "NULL", note: "" },
          ...BASE_COLS,
        ],
        indexes: ["UX_Catalog_TrackFiles_TrackId", "IX_Catalog_TrackFiles_Status"],
        fkNote: null,
      },
      {
        name: "Genres",
        fullName: "Catalog.Genres",
        icon: "🏷️",
        purpose: "Genre and mood tags. Admin-managed. Type = 0 Genre / 1 Mood.",
        redisNote: null,
        columns: [
          { name: "Id", type: "INT", constraints: "PK IDENTITY(1,1)", note: "" },
          { name: "Name", type: "NVARCHAR(50)", constraints: "NOT NULL UNIQUE", note: "" },
          { name: "Slug", type: "VARCHAR(50)", constraints: "NOT NULL UNIQUE", note: "e.g. hip-hop" },
          { name: "Type", type: "TINYINT", constraints: "NOT NULL", note: "0=Genre 1=Mood" },
          { name: "ColorHex", type: "VARCHAR(7)", constraints: "NULL", note: "" },
          ...BASE_COLS,
        ],
        indexes: ["UX_Catalog_Genres_Slug", "IX_Catalog_Genres_Type"],
        fkNote: null,
      },
      {
        name: "AlbumArtists",
        fullName: "Catalog.AlbumArtists",
        icon: "🔗",
        purpose: "Album ↔ Artist M:M junction. IsPrimary marks the main credited artist.",
        redisNote: null,
        columns: [
          { name: "AlbumId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Catalog.Albums"), note: "PK (composite)" },
          { name: "ArtistId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Catalog.Artists"), note: "PK (composite)" },
          { name: "IsPrimary", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "DisplayOrder", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "" },
        ],
        indexes: ["IX_Catalog_AlbumArtists_ArtistId"],
        fkNote: null,
      },
      {
        name: "TrackArtists",
        fullName: "Catalog.TrackArtists",
        icon: "🔗",
        purpose: "Track ↔ Artist M:M junction. Role = Primary / Featured / Remixer.",
        redisNote: null,
        columns: [
          { name: "TrackId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Catalog.Tracks"), note: "PK (composite)" },
          { name: "ArtistId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Catalog.Artists"), note: "PK (composite)" },
          { name: "Role", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "0=Primary 1=Featured 2=Remixer" },
          { name: "DisplayOrder", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "" },
        ],
        indexes: ["IX_Catalog_TrackArtists_ArtistId"],
        fkNote: null,
      },
      {
        name: "TrackGenres",
        fullName: "Catalog.TrackGenres",
        icon: "🔗",
        purpose: "Track ↔ Genre/Mood M:M junction.",
        redisNote: null,
        columns: [
          { name: "TrackId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Catalog.Tracks"), note: "PK (composite)" },
          { name: "GenreId", type: "INT", constraints: FK.intra("Catalog.Genres"), note: "PK (composite)" },
        ],
        indexes: ["IX_Catalog_TrackGenres_GenreId"],
        fkNote: null,
      },
      {
        name: "AlbumGenres",
        fullName: "Catalog.AlbumGenres",
        icon: "🔗",
        purpose: "Album ↔ Genre/Mood M:M junction.",
        redisNote: null,
        columns: [
          { name: "AlbumId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Catalog.Albums"), note: "PK (composite)" },
          { name: "GenreId", type: "INT", constraints: FK.intra("Catalog.Genres"), note: "PK (composite)" },
        ],
        indexes: ["IX_Catalog_AlbumGenres_GenreId"],
        fkNote: null,
      },
    ],
  },

  // ─────────────────────────────────────────────────────────────────────────
  {
    id: "streaming",
    schema: "Streaming",
    label: "Streaming",
    color: "#8B5CF6",
    icon: "▶️",
    desc: "Playback events, queue sessions, search history. References Identity and Catalog cross-module.",
    tables: [
      {
        name: "PlayHistory",
        fullName: "Streaming.PlayHistory",
        icon: "▶️",
        purpose: "Append-only log of every play event. Source of truth for analytics + Qdrant vectors. BIGINT PK for volume.",
        redisNote: "playback_pos:{userId}:{trackId} → TTL 30 days (resume position)\nplay_count:{trackId} → INCR per play, flushed to Catalog.Tracks.PlayCount every 5 min",
        columns: [
          { name: "Id", type: "BIGINT", constraints: "PK IDENTITY(1,1)", note: "BIGINT — high volume" },
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "Cross-module — no FK constraint" },
          { name: "TrackId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Catalog.Tracks"), note: "Cross-module — no FK constraint" },
          { name: "PlayedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
          { name: "DurationPlayedMs", type: "INT", constraints: "NOT NULL", note: "Actual listen duration" },
          { name: "WasSkipped", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "1 if < 30s — Qdrant negative signal" },
          { name: "DeviceType", type: "TINYINT", constraints: "NULL", note: "0=Web 1=Mobile 2=Desktop" },
          { name: "ContextType", type: "TINYINT", constraints: "NULL", note: "0=Playlist 1=Album 2=Artist 3=Search 4=Direct" },
          { name: "ContextId", type: "UNIQUEIDENTIFIER", constraints: "NULL", note: "Id of the playlist/album/artist" },
        ],
        indexes: [
          "IX_Streaming_PlayHistory_UserId_PlayedAt DESC",
          "IX_Streaming_PlayHistory_TrackId_PlayedAt DESC",
          "IX_Streaming_PlayHistory_PlayedAt DESC",
        ],
        fkNote: "UserId → Identity.Users and TrackId → Catalog.Tracks are cross-module. Stored as plain IDs. No FK constraints.",
      },
      {
        name: "QueueSessions",
        fullName: "Streaming.QueueSessions",
        icon: "⏭️",
        purpose: "One row per user. Durable backup of their active queue. Redis holds the live version; DB is the fallback.",
        redisNote: "queue:{userId} → JSON array of TrackIds, synced from DB on session start, written back periodically",
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users") + " UNIQUE", note: "Cross-module — one row per user" },
          { name: "CurrentTrackId", type: "UNIQUEIDENTIFIER", constraints: FK.crossOpt("Catalog.Tracks"), note: "Cross-module" },
          { name: "CurrentPositionMs", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "QueueJson", type: "NVARCHAR(MAX)", constraints: "NULL", note: "JSON array of TrackIds" },
          { name: "ShuffleEnabled", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "RepeatMode", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "0=Off 1=RepeatAll 2=RepeatOne" },
          { name: "ContextType", type: "TINYINT", constraints: "NULL", note: "" },
          { name: "ContextId", type: "UNIQUEIDENTIFIER", constraints: "NULL", note: "" },
          { name: "UpdatedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["UX_Streaming_QueueSessions_UserId"],
        fkNote: "UserId and CurrentTrackId are cross-module. No FK constraints.",
      },
      {
        name: "SearchHistory",
        fullName: "Streaming.SearchHistory",
        icon: "🔍",
        purpose: "Per-user recent search queries for autocomplete. Max 50 rows per user enforced by application.",
        redisNote: null,
        columns: [
          { name: "Id", type: "INT", constraints: "PK IDENTITY(1,1)", note: "" },
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "Cross-module" },
          { name: "Query", type: "NVARCHAR(200)", constraints: "NOT NULL", note: "" },
          { name: "SearchedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Streaming_SearchHistory_UserId_SearchedAt DESC"],
        fkNote: "UserId is cross-module. No FK constraint.",
      },
    ],
  },

  // ─────────────────────────────────────────────────────────────────────────
  {
    id: "playlist",
    schema: "Playlist",
    label: "Playlist",
    color: "#3B82F6",
    icon: "📋",
    desc: "Playlists, track ordering, collaborators, and all liked/saved content.",
    tables: [
      {
        name: "Playlists",
        fullName: "Playlist.Playlists",
        icon: "📋",
        purpose: "User and system playlists. IsSystem=1 protects Liked Songs from deletion.",
        redisNote: null,
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "OwnerId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "Cross-module" },
          { name: "Title", type: "NVARCHAR(200)", constraints: "NOT NULL", note: "" },
          { name: "Description", type: "NVARCHAR(500)", constraints: "NULL", note: "" },
          { name: "CoverArtUrl", type: "NVARCHAR(500)", constraints: "NULL", note: "Auto-generated from first 4 tracks if NULL" },
          { name: "Visibility", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "0=Private 1=Public 2=Collaborative" },
          { name: "IsSystem", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "1 = Liked Songs — delete returns 403" },
          { name: "TrackCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "Denormalized" },
          { name: "TotalDurationMs", type: "BIGINT", constraints: "NOT NULL DEFAULT 0", note: "Denormalized" },
          { name: "FollowerCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "Denormalized" },
          ...BASE_COLS,
        ],
        indexes: ["IX_Playlist_Playlists_OwnerId", "IX_Playlist_Playlists_Visibility", "IX_Playlist_Playlists_IsSystem"],
        fkNote: "OwnerId is cross-module. No FK constraint.",
      },
      {
        name: "PlaylistTracks",
        fullName: "Playlist.PlaylistTracks",
        icon: "🔗",
        purpose: "Ordered track membership. Position enables drag-and-drop reorder.",
        redisNote: null,
        columns: [
          { name: "PlaylistId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Playlist.Playlists"), note: "PK (composite)" },
          { name: "TrackId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Catalog.Tracks"), note: "PK (composite) — cross-module" },
          { name: "Position", type: "INT", constraints: "NOT NULL", note: "0-based ordering" },
          { name: "AddedByUserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "Cross-module — who added in collab playlists" },
          { name: "AddedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Playlist_PlaylistTracks_PlaylistId_Position", "IX_Playlist_PlaylistTracks_TrackId"],
        fkNote: "TrackId → Catalog.Tracks and AddedByUserId → Identity.Users are cross-module. No FK constraints.",
      },
      {
        name: "PlaylistCollaborators",
        fullName: "Playlist.PlaylistCollaborators",
        icon: "👥",
        purpose: "Who can edit a Collaborative playlist beyond the owner.",
        redisNote: null,
        columns: [
          { name: "PlaylistId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Playlist.Playlists"), note: "PK (composite)" },
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "PK (composite) — cross-module" },
          { name: "PermissionLevel", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "0=AddOnly 1=AddAndRemove" },
          { name: "InvitedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Playlist_PlaylistCollaborators_UserId"],
        fkNote: "UserId is cross-module. No FK constraint.",
      },
      {
        name: "LikedTracks",
        fullName: "Playlist.LikedTracks",
        icon: "❤️",
        purpose: "Track likes per user. On insert: also add to user's Liked Songs playlist in PlaylistTracks.",
        redisNote: null,
        columns: [
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "PK (composite) — cross-module" },
          { name: "TrackId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Catalog.Tracks"), note: "PK (composite) — cross-module" },
          { name: "LikedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Playlist_LikedTracks_TrackId", "IX_Playlist_LikedTracks_UserId_LikedAt DESC"],
        fkNote: "Both columns are cross-module. No FK constraints.",
      },
      {
        name: "LikedAlbums",
        fullName: "Playlist.LikedAlbums",
        icon: "❤️",
        purpose: "Album saves per user. Populates Library > Albums tab.",
        redisNote: null,
        columns: [
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "PK (composite) — cross-module" },
          { name: "AlbumId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Catalog.Albums"), note: "PK (composite) — cross-module" },
          { name: "LikedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Playlist_LikedAlbums_AlbumId"],
        fkNote: "Both columns are cross-module. No FK constraints.",
      },
      {
        name: "LikedPlaylists",
        fullName: "Playlist.LikedPlaylists",
        icon: "❤️",
        purpose: "Playlist follows per user. Distinct from PlaylistCollaborators (following ≠ editing).",
        redisNote: null,
        columns: [
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "PK (composite) — cross-module" },
          { name: "PlaylistId", type: "UNIQUEIDENTIFIER", constraints: FK.intra("Playlist.Playlists"), note: "PK (composite)" },
          { name: "LikedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Playlist_LikedPlaylists_PlaylistId"],
        fkNote: "UserId is cross-module. No FK constraint.",
      },
    ],
  },

  // ─────────────────────────────────────────────────────────────────────────
  {
    id: "social",
    schema: "Social",
    label: "Social",
    color: "#F97316",
    icon: "👥",
    desc: "Following, artist posts, notifications. All references to Identity and Catalog are cross-module.",
    tables: [
      {
        name: "UserFollows",
        fullName: "Social.UserFollows",
        icon: "👤→👤",
        purpose: "User following another user. On insert/delete update Identity.Users follower/following counts.",
        redisNote: null,
        columns: [
          { name: "FollowerId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "PK (composite) — cross-module" },
          { name: "FolloweeId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "PK (composite) — cross-module" },
          { name: "CreatedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Social_UserFollows_FolloweeId", "CHECK: FollowerId <> FolloweeId"],
        fkNote: "Both columns are cross-module. No FK constraints.",
      },
      {
        name: "ArtistFollows",
        fullName: "Social.ArtistFollows",
        icon: "👤→🎤",
        purpose: "User following an artist. On insert/delete update Catalog.Artists.FollowerCount.",
        redisNote: null,
        columns: [
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "PK (composite) — cross-module" },
          { name: "ArtistId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Catalog.Artists"), note: "PK (composite) — cross-module" },
          { name: "CreatedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Social_ArtistFollows_ArtistId"],
        fkNote: "Both columns are cross-module. No FK constraints.",
      },
      {
        name: "ArtistPosts",
        fullName: "Social.ArtistPosts",
        icon: "📝",
        purpose: "Artist update feed posts. Shown to followers on home feed.",
        redisNote: null,
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "ArtistId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Catalog.Artists"), note: "Cross-module" },
          { name: "Body", type: "NVARCHAR(2000)", constraints: "NOT NULL", note: "" },
          { name: "ImageUrl", type: "NVARCHAR(500)", constraints: "NULL", note: "" },
          { name: "LinkedTrackId", type: "UNIQUEIDENTIFIER", constraints: FK.crossOpt("Catalog.Tracks"), note: "Cross-module — optional promo link" },
          { name: "LinkedAlbumId", type: "UNIQUEIDENTIFIER", constraints: FK.crossOpt("Catalog.Albums"), note: "Cross-module — optional promo link" },
          ...BASE_COLS,
        ],
        indexes: ["IX_Social_ArtistPosts_ArtistId_CreatedDate DESC"],
        fkNote: "All entity references are cross-module. No FK constraints.",
      },
      {
        name: "Notifications",
        fullName: "Social.Notifications",
        icon: "🔔",
        purpose: "In-app notification bell. Written by RabbitMQ consumers on domain events.",
        redisNote: null,
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "Cross-module — recipient" },
          { name: "Type", type: "TINYINT", constraints: "NOT NULL", note: "0=ArtistApproved 1=NewFollower 2=TrackReady 3=CollabInvite 4=NewArtistPost" },
          { name: "Title", type: "NVARCHAR(200)", constraints: "NOT NULL", note: "" },
          { name: "Body", type: "NVARCHAR(500)", constraints: "NULL", note: "" },
          { name: "ReferenceType", type: "NVARCHAR(50)", constraints: "NULL", note: "Track / Artist / Playlist / Post" },
          { name: "ReferenceId", type: "UNIQUEIDENTIFIER", constraints: "NULL", note: "Id of referenced entity — cross-module" },
          { name: "IsRead", type: "BIT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "ReadAt", type: "DATETIME2", constraints: "NULL", note: "" },
          { name: "CreatedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Social_Notifications_UserId_IsRead", "IX_Social_Notifications_UserId_CreatedAt DESC"],
        fkNote: "UserId and ReferenceId are cross-module. No FK constraints.",
      },
    ],
  },

  // ─────────────────────────────────────────────────────────────────────────
  {
    id: "analytics",
    schema: "Analytics",
    label: "Analytics",
    color: "#F59E0B",
    icon: "📊",
    desc: "Read-only aggregate tables. Populated nightly by Worker jobs from Streaming.PlayHistory. Never written to by API handlers.",
    tables: [
      {
        name: "DailyTrackStats",
        fullName: "Analytics.DailyTrackStats",
        icon: "📈",
        purpose: "Daily stream snapshot per track. Powers artist dashboard track performance chart.",
        redisNote: null,
        columns: [
          { name: "Id", type: "BIGINT", constraints: "PK IDENTITY(1,1)", note: "" },
          { name: "TrackId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Catalog.Tracks"), note: "Cross-module" },
          { name: "Date", type: "DATE", constraints: "NOT NULL", note: "" },
          { name: "StreamCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "UniqueListeners", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "SkipCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "TotalListenMs", type: "BIGINT", constraints: "NOT NULL DEFAULT 0", note: "" },
        ],
        indexes: ["UX_Analytics_DailyTrackStats_TrackId_Date", "IX_Analytics_DailyTrackStats_Date DESC"],
        fkNote: "TrackId is cross-module. No FK constraint.",
      },
      {
        name: "DailyArtistStats",
        fullName: "Analytics.DailyArtistStats",
        icon: "📈",
        purpose: "Daily rollup per artist. Powers streams, unique listeners, follower gain/loss chart.",
        redisNote: null,
        columns: [
          { name: "Id", type: "BIGINT", constraints: "PK IDENTITY(1,1)", note: "" },
          { name: "ArtistId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Catalog.Artists"), note: "Cross-module" },
          { name: "Date", type: "DATE", constraints: "NOT NULL", note: "" },
          { name: "StreamCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "UniqueListeners", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "FollowerGained", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "FollowerLost", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "TopTrackId", type: "UNIQUEIDENTIFIER", constraints: FK.crossOpt("Catalog.Tracks"), note: "Cross-module" },
        ],
        indexes: ["UX_Analytics_DailyArtistStats_ArtistId_Date", "IX_Analytics_DailyArtistStats_Date DESC"],
        fkNote: "ArtistId and TopTrackId are cross-module. No FK constraints.",
      },
      {
        name: "MonthlyUserStats",
        fullName: "Analytics.MonthlyUserStats",
        icon: "📊",
        purpose: "Monthly listening rollup per user. Powers personal stats page and Spotify Wrapped equivalent.",
        redisNote: null,
        columns: [
          { name: "Id", type: "BIGINT", constraints: "PK IDENTITY(1,1)", note: "" },
          { name: "UserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "Cross-module" },
          { name: "Year", type: "SMALLINT", constraints: "NOT NULL", note: "" },
          { name: "Month", type: "TINYINT", constraints: "NOT NULL", note: "1–12" },
          { name: "TotalListenMs", type: "BIGINT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "TotalTracksPlayed", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "UniqueTracksPlayed", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "UniqueArtistsListened", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "TopTrackId", type: "UNIQUEIDENTIFIER", constraints: FK.crossOpt("Catalog.Tracks"), note: "Cross-module" },
          { name: "TopArtistId", type: "UNIQUEIDENTIFIER", constraints: FK.crossOpt("Catalog.Artists"), note: "Cross-module" },
          { name: "TopGenreId", type: "INT", constraints: FK.crossOpt("Catalog.Genres"), note: "Cross-module" },
        ],
        indexes: ["UX_Analytics_MonthlyUserStats_UserId_Year_Month", "IX_Analytics_MonthlyUserStats_UserId DESC"],
        fkNote: "All entity references are cross-module. No FK constraints.",
      },
      {
        name: "PlatformDailyStats",
        fullName: "Analytics.PlatformDailyStats",
        icon: "🌐",
        purpose: "Platform-wide daily metrics. One row per day. Powers admin dashboard.",
        redisNote: null,
        columns: [
          { name: "Id", type: "INT", constraints: "PK IDENTITY(1,1)", note: "" },
          { name: "Date", type: "DATE", constraints: "NOT NULL UNIQUE", note: "" },
          { name: "DailyActiveUsers", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "Distinct users with ≥1 play" },
          { name: "NewRegistrations", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "TotalStreams", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "TotalUploads", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "FailedProcessingCount", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "FFmpeg failures" },
          { name: "TotalListenMs", type: "BIGINT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "NewArtists", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
          { name: "NewAlbums", type: "INT", constraints: "NOT NULL DEFAULT 0", note: "" },
        ],
        indexes: ["UX_Analytics_PlatformDailyStats_Date"],
        fkNote: null,
      },
    ],
  },

  // ─────────────────────────────────────────────────────────────────────────
  {
    id: "infra",
    schema: "Infra",
    label: "Infra",
    color: "#06B6D4",
    icon: "⚙️",
    desc: "Outbox messages and admin audit logs. Cross-cutting concerns shared by all modules.",
    tables: [
      {
        name: "OutboxMessages",
        fullName: "Infra.OutboxMessages",
        icon: "📬",
        purpose: "Transactional outbox. Written in same EF transaction as business data. OutboxProcessor Worker publishes to RabbitMQ.",
        redisNote: null,
        columns: [
          { name: "Id", type: "UNIQUEIDENTIFIER", constraints: "PK DEFAULT NEWID()", note: "" },
          { name: "Type", type: "NVARCHAR(200)", constraints: "NOT NULL", note: "e.g. TrackUploaded, UserRegistered" },
          { name: "RoutingKey", type: "NVARCHAR(200)", constraints: "NOT NULL", note: "RabbitMQ routing key" },
          { name: "Payload", type: "NVARCHAR(MAX)", constraints: "NOT NULL", note: "JSON event body" },
          { name: "OccurredAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
          { name: "ProcessedAt", type: "DATETIME2", constraints: "NULL", note: "Set by Worker on success" },
          { name: "RetryCount", type: "TINYINT", constraints: "NOT NULL DEFAULT 0", note: "Max 3 then dead" },
          { name: "LastError", type: "NVARCHAR(500)", constraints: "NULL", note: "" },
        ],
        indexes: ["IX_Infra_OutboxMessages_ProcessedAt (filtered: WHERE ProcessedAt IS NULL)"],
        fkNote: null,
      },
      {
        name: "AuditLogs",
        fullName: "Infra.AuditLogs",
        icon: "📋",
        purpose: "Admin action audit trail with before/after JSON snapshots. Append-only.",
        redisNote: null,
        columns: [
          { name: "Id", type: "BIGINT", constraints: "PK IDENTITY(1,1)", note: "BIGINT — append-only" },
          { name: "ActorUserId", type: "UNIQUEIDENTIFIER", constraints: FK.cross("Identity.Users"), note: "Cross-module" },
          { name: "Action", type: "NVARCHAR(100)", constraints: "NOT NULL", note: "ApproveArtist / LockUser / DeleteTrack" },
          { name: "TargetEntityType", type: "NVARCHAR(50)", constraints: "NOT NULL", note: "User / Artist / Track / Album" },
          { name: "TargetEntityId", type: "UNIQUEIDENTIFIER", constraints: "NOT NULL", note: "Cross-module entity id" },
          { name: "OldValues", type: "NVARCHAR(MAX)", constraints: "NULL", note: "JSON snapshot before" },
          { name: "NewValues", type: "NVARCHAR(MAX)", constraints: "NULL", note: "JSON snapshot after" },
          { name: "IpAddress", type: "VARCHAR(45)", constraints: "NULL", note: "" },
          { name: "CreatedAt", type: "DATETIME2", constraints: "NOT NULL DEFAULT SYSUTCDATETIME()", note: "" },
        ],
        indexes: ["IX_Infra_AuditLogs_ActorUserId", "IX_Infra_AuditLogs_TargetEntityId", "IX_Infra_AuditLogs_CreatedAt DESC"],
        fkNote: "ActorUserId is cross-module. No FK constraint.",
      },
    ],
  },
];

// ─── Components ───────────────────────────────────────────────────────────────
const BASE_ENT_COLOR = "#1e3a4a";

function colColor(constraints) {
  if (constraints.includes("PK")) return "#F59E0B";
  if (constraints.includes("✦")) return "#1DB954";   // intra-module FK
  if (constraints.includes("✧")) return "#F97316";   // cross-module ref
  return "#cbd5e1";
}

function TableCard({ table, moduleColor, showBase, onToggleBase }) {
  const visibleCols = showBase ? table.columns : table.columns.filter(c => c.note !== BASE_ENTITY_NOTE);
  const baseCols = table.columns.filter(c => c.note === BASE_ENTITY_NOTE);
  const crossCols = table.columns.filter(c => c.constraints && c.constraints.includes("✧"));

  return (
    <div style={{ background: "#0c0c0c", border: `1px solid ${moduleColor}33`, borderRadius: 12, overflow: "hidden", marginBottom: 16 }}>
      {/* Header */}
      <div style={{ padding: "14px 20px", background: "#111", borderBottom: "1px solid #1a1a1a", display: "flex", alignItems: "flex-start", gap: 12 }}>
        <span style={{ fontSize: 20, marginTop: 1 }}>{table.icon}</span>
        <div style={{ flex: 1 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 10, flexWrap: "wrap", marginBottom: 3 }}>
            <code style={{ fontWeight: 800, fontSize: 15, color: moduleColor }}>{table.fullName}</code>
            <span style={{ fontSize: 11, color: "#334155" }}>{table.columns.length} cols</span>
            {crossCols.length > 0 && (
              <span style={{ background: "rgba(249,115,22,0.12)", color: "#F97316", border: "1px solid rgba(249,115,22,0.3)", borderRadius: 8, padding: "1px 8px", fontSize: 10, fontWeight: 700 }}>{crossCols.length} cross-module ref{crossCols.length > 1 ? "s" : ""}</span>
            )}
          </div>
          <div style={{ fontSize: 12, color: "#64748b", lineHeight: 1.5 }}>{table.purpose}</div>
        </div>
      </div>

      {/* Redis note */}
      {table.redisNote && (
        <div style={{ padding: "10px 20px", background: "rgba(6,182,212,0.04)", borderBottom: "1px solid rgba(6,182,212,0.12)" }}>
          <div style={{ fontSize: 10, fontWeight: 700, letterSpacing: 2, color: "#06B6D4", marginBottom: 5, textTransform: "uppercase" }}>Redis (not in DB)</div>
          {table.redisNote.split("\n").map((l, i) => <div key={i} style={{ fontFamily: "monospace", fontSize: 11, color: "#0e7490", marginBottom: 2 }}>{l}</div>)}
        </div>
      )}

      {/* Columns */}
      <div>
        <div style={{ display: "grid", gridTemplateColumns: "180px 150px 1fr 1fr", padding: "5px 20px 6px", borderBottom: "1px solid #111" }}>
          {["Column", "Type", "Constraints", "Note"].map(h => (
            <div key={h} style={{ fontSize: 10, fontWeight: 700, letterSpacing: 2, color: "#1e293b", textTransform: "uppercase" }}>{h}</div>
          ))}
        </div>
        {visibleCols.map((col, i) => (
          <div key={col.name} style={{ display: "grid", gridTemplateColumns: "180px 150px 1fr 1fr", padding: "5px 20px", background: i % 2 === 0 ? "transparent" : "#080808" }}>
            <code style={{ fontSize: 13, fontWeight: 600, color: colColor(col.constraints) }}>{col.name}</code>
            <code style={{ fontSize: 11, color: "#334155" }}>{col.type}</code>
            <div style={{ fontSize: 11, color: col.constraints.includes("✧") ? "#F97316" : col.constraints.includes("✦") ? "#1DB954" : "#334155", paddingRight: 12, lineHeight: 1.4 }}>{col.constraints}</div>
            <div style={{ fontSize: 11, color: "#1e293b" }}>{col.note}</div>
          </div>
        ))}

        {/* BaseEntity toggle */}
        {baseCols.length > 0 && (
          <div style={{ padding: "7px 20px", borderTop: "1px solid #0f172a" }}>
            <button onClick={onToggleBase} style={{ background: "none", border: "1px solid #0f172a", borderRadius: 6, padding: "3px 12px", color: "#1e293b", fontSize: 11, cursor: "pointer", fontFamily: "monospace" }}>
              {showBase ? `▲ hide BaseEntity (${baseCols.length})` : `▼ show BaseEntity (${baseCols.length}): CreatedBy · CreatedDate · UpdatedBy · UpdatedDate · IsDeleted`}
            </button>
          </div>
        )}
      </div>

      {/* Footer */}
      <div style={{ padding: "10px 20px 12px", borderTop: "1px solid #111", background: "#080808", display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
        <div>
          <div style={{ fontSize: 10, fontWeight: 700, letterSpacing: 2, color: "#0f172a", textTransform: "uppercase", marginBottom: 5 }}>Indexes</div>
          {table.indexes.map(ix => <div key={ix} style={{ fontSize: 11, color: "#1e293b", fontFamily: "monospace", marginBottom: 2, lineHeight: 1.4 }}>• {ix}</div>)}
        </div>
        {table.fkNote && (
          <div>
            <div style={{ fontSize: 10, fontWeight: 700, letterSpacing: 2, color: "#F97316", textTransform: "uppercase", marginBottom: 5 }}>Cross-Module Rule</div>
            <div style={{ fontSize: 11, color: "#7c3f00", lineHeight: 1.5 }}>{table.fkNote}</div>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── App ──────────────────────────────────────────────────────────────────────
export default function App() {
  const [activeId, setActiveId] = useState("identity");
  const [search, setSearch] = useState("");
  const [showBase, setShowBase] = useState({});
  const [view, setView] = useState("tables"); // tables | map

  const allTables = modules.flatMap(m => m.tables.map(t => ({ ...t, moduleColor: m.color, moduleName: m.label, moduleSchema: m.schema })));
  const filtered = search.trim()
    ? allTables.filter(t =>
        t.name.toLowerCase().includes(search.toLowerCase()) ||
        t.fullName.toLowerCase().includes(search.toLowerCase()) ||
        t.columns.some(c => c.name.toLowerCase().includes(search.toLowerCase()))
      )
    : null;

  const activeModule = modules.find(m => m.id === activeId);
  const displayTables = filtered || activeModule.tables.map(t => ({ ...t, moduleColor: activeModule.color }));

  const totalTables = allTables.length;
  const totalCols = allTables.reduce((a, t) => a + t.columns.length, 0);

  function toggleBase(name) { setShowBase(p => ({ ...p, [name]: !p[name] })); }

  return (
    <div style={{ fontFamily: "'Inter', -apple-system, sans-serif", background: "#020409", minHeight: "100vh", color: "#fff", display: "flex" }}>

      {/* Sidebar */}
      <div style={{ width: 240, borderRight: "1px solid #0f172a", padding: "20px 12px", flexShrink: 0, display: "flex", flexDirection: "column", gap: 3, overflowY: "auto", maxHeight: "100vh" }}>
        <div style={{ padding: "0 8px 16px", borderBottom: "1px solid #0f172a", marginBottom: 8 }}>
          <div style={{ fontSize: 10, fontWeight: 800, letterSpacing: 3, color: "#1DB954", textTransform: "uppercase" }}>SoundWave</div>
          <div style={{ fontSize: 15, fontWeight: 800, color: "#fff", marginTop: 2 }}>DB Schema v5</div>
          <div style={{ fontSize: 11, color: "#1e293b", marginTop: 3 }}>Modular Monolith · {modules.length} schemas · {totalTables} tables</div>
        </div>

        <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search tables / columns..."
          style={{ width: "100%", background: "#0f172a", border: "1px solid #1e293b", borderRadius: 8, padding: "7px 10px", color: "#94a3b8", fontSize: 12, outline: "none", boxSizing: "border-box", marginBottom: 8 }} />

        {!search && modules.map(m => (
          <button key={m.id} onClick={() => setActiveId(m.id)}
            style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "9px 12px", borderRadius: 8, border: "none", background: activeId === m.id ? `${m.color}15` : "transparent", color: activeId === m.id ? m.color : "#475569", cursor: "pointer", fontSize: 13, fontWeight: activeId === m.id ? 700 : 400, textAlign: "left", borderLeft: activeId === m.id ? `3px solid ${m.color}` : "3px solid transparent", transition: "all 0.15s" }}>
            <span style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <span>{m.icon}</span>
              <div>
                <div>{m.label}</div>
                <div style={{ fontSize: 10, color: activeId === m.id ? m.color + "99" : "#334155", fontFamily: "monospace" }}>{m.schema}.*</div>
              </div>
            </span>
            <span style={{ fontSize: 11, opacity: 0.4 }}>{m.tables.length}</span>
          </button>
        ))}

        {/* FK legend */}
        {!search && (
          <div style={{ marginTop: "auto", padding: "16px 8px 0", borderTop: "1px solid #0f172a" }}>
            <div style={{ fontSize: 10, fontWeight: 700, letterSpacing: 2, color: "#1e293b", textTransform: "uppercase", marginBottom: 10 }}>FK Legend</div>
            {[
              { color: "#F59E0B", symbol: "—", label: "Primary Key" },
              { color: "#1DB954", symbol: "✦", label: "Intra-module FK (enforced)" },
              { color: "#F97316", symbol: "✧", label: "Cross-module ref (ID only)" },
              { color: "#cbd5e1", symbol: "—", label: "Regular column" },
            ].map(l => (
              <div key={l.label} style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 6 }}>
                <div style={{ width: 18, textAlign: "center", fontFamily: "monospace", fontSize: 13, color: l.color }}>{l.symbol}</div>
                <span style={{ fontSize: 11, color: "#334155" }}>{l.label}</span>
              </div>
            ))}
            <div style={{ marginTop: 12, padding: "10px 12px", background: "#0f172a", borderRadius: 8, fontSize: 11, color: "#334155", lineHeight: 1.6 }}>
              <strong style={{ color: "#1DB954" }}>✦ Intra:</strong> Real FOREIGN KEY constraint in MSSQL.<br />
              <strong style={{ color: "#F97316" }}>✧ Cross:</strong> Plain UNIQUEIDENTIFIER. No FK constraint. Consistency enforced by application layer.
            </div>
          </div>
        )}
      </div>

      {/* Main */}
      <div style={{ flex: 1, padding: 28, overflowY: "auto", maxHeight: "100vh" }}>
        <div style={{ maxWidth: 1060 }}>
          {/* Header */}
          {!search && (
            <div style={{ marginBottom: 24 }}>
              <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 6 }}>
                <span style={{ fontSize: 24 }}>{activeModule.icon}</span>
                <div>
                  <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                    <h1 style={{ fontSize: 22, fontWeight: 800, color: "#fff", margin: 0 }}>{activeModule.label}</h1>
                    <code style={{ fontSize: 13, color: activeModule.color, background: activeModule.color + "15", padding: "2px 10px", borderRadius: 6 }}>{activeModule.schema}.*</code>
                    <span style={{ fontSize: 12, color: "#334155" }}>{activeModule.tables.length} tables</span>
                  </div>
                  <div style={{ fontSize: 13, color: "#475569", marginTop: 4 }}>{activeModule.desc}</div>
                </div>
              </div>
            </div>
          )}
          {search && (
            <div style={{ marginBottom: 20, display: "flex", alignItems: "center", gap: 10 }}>
              <h1 style={{ fontSize: 20, fontWeight: 800, color: "#fff", margin: 0 }}>"{search}"</h1>
              <span style={{ fontSize: 13, color: "#334155" }}>{displayTables.length} result{displayTables.length !== 1 ? "s" : ""}</span>
            </div>
          )}

          {/* Tables */}
          {displayTables.map(t => (
            <div key={t.fullName || t.name}>
              {search && <div style={{ fontSize: 10, fontWeight: 700, letterSpacing: 2, color: t.moduleColor, marginBottom: 6, textTransform: "uppercase" }}>▸ {t.moduleSchema || t.moduleName}</div>}
              <TableCard table={t} moduleColor={t.moduleColor} showBase={!!showBase[t.name]} onToggleBase={() => toggleBase(t.name)} />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
