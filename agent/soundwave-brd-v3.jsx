import { useState } from "react";

const sections = [
  {
    id: "overview",
    icon: "🎵",
    title: "Project Overview",
    color: "#1DB954",
    content: {
      type: "overview",
      data: {
        name: "SoundWave – Spotify Clone",
        stack: [".NET 8 Web API", "React + Vite", "MSSQL (7 schemas)", "Redis", "RabbitMQ.Client", "FFmpeg (HLS)", "JWT Auth", "Serilog + Seq", "xUnit + Moq", "ElasticSearch 8", "Qdrant", "Guid.CreateVersion7()"],
        goal: "Full-featured Spotify clone. Modular Monolith with Vertical Slice Architecture + CQRS. Event-driven audio processing via RabbitMQ (raw client, no MassTransit). Local disk storage in dev via IFileStorage abstraction — swap to Azure/S3 in prod with zero code changes. Sequential GUIDs everywhere. No cross-schema FK constraints — module independence enforced by application layer.",
        phases: [
          { label: "Phase 0 – Study", items: ["RabbitMQ, Modular Monolith, CQRS, EF Core schemas, FFmpeg, Redis patterns, Docker Compose stack"] },
          { label: "Phase 1 – Core", items: ["Auth (Identity), Catalog (Artists/Albums/Tracks), Playlists, Outbox Worker, React shell"] },
          { label: "Phase 2 – Streaming", items: ["IFileStorage (local disk), Upload pipeline, FFmpeg → HLS, Play events, Queue sessions, hls.js player"] },
          { label: "Phase 3 – Social", items: ["Following, Notifications, Artist posts, Home feed, Collaborative playlists"] },
          { label: "Phase 4 – Analytics", items: ["Nightly rollup Worker, Artist dashboard, User stats, Admin dashboard, Audit logs"] },
          { label: "Phase 5 – Discovery", items: ["ElasticSearch indexing + search, Qdrant embeddings + recommendations"] },
          { label: "Phase 6 – Polish", items: ["Integration tests (Testcontainers), security hardening, observability, frontend polish, prod file storage"] },
        ]
      }
    }
  },
  {
    id: "upload",
    icon: "📤",
    title: "Upload & HLS Pipeline",
    color: "#F97316",
    content: { type: "upload" }
  },
  {
    id: "architecture",
    icon: "🏗️",
    title: "Architecture",
    color: "#06B6D4",
    content: {
      type: "architecture",
      data: {
        layers: [
          {
            name: "Domain",
            color: "#F59E0B",
            desc: "Entities, errors, interfaces. Zero dependencies.",
            contents: ["Track, Artist, Album, Playlist, User entities", "TrackStatus enum (Pending | Processing | Ready | Failed)", "ITrackRepository, IUserRepository, IFileStorage", "Domain errors (sealed classes)", "No EF Core, no MediatR"]
          },
          {
            name: "Application",
            color: "#8B5CF6",
            desc: "CQRS via MediatR, FluentValidation pipeline, Result<T>.",
            contents: ["UploadTrackCommand → saves raw file → publishes event", "Commands / Queries per feature", "IEventBus (publish abstraction)", "IFileStorage (storage abstraction)", "ValidationBehavior (MediatR pipeline)", "Result<T> / Error — no thrown exceptions"]
          },
          {
            name: "Infrastructure",
            color: "#3B82F6",
            desc: "EF Core + MSSQL, Redis, RabbitMQ.Client, local disk, FFmpeg.",
            contents: ["AppDbContext (EF Core, MSSQL)", "LocalFileStorage : IFileStorage (dev)", "RabbitMQ.Client — raw publish/consume", "HLS Consumer (FFmpeg wrapper)", "StackExchange.Redis", "Serilog config", "JWT token service", "OutboxProcessor (Worker Service)"]
          },
          {
            name: "Presentation",
            color: "#1DB954",
            desc: "ASP.NET Core controllers, middleware, DI registration.",
            contents: ["Thin controllers — delegate to MediatR", "Global exception middleware", "JWT middleware + role authorization", "Rate limiting middleware", "Static file serving (dev: /uploads, /hls)", "Health check endpoints", "Swagger / OpenAPI"]
          }
        ],
        patterns: [
          { name: "CQRS + MediatR", note: "Commands mutate state, queries return data. Strict separation. Handlers in Application layer." },
          { name: "Result<T> Pattern", note: "All operations return Result<T> or Error. No exception-driven flow in business logic." },
          { name: "FluentValidation Pipeline", note: "MediatR IPipelineBehavior validates every command before handler executes." },
          { name: "Global Exception Middleware", note: "Catches unhandled exceptions, maps to ProblemDetails RFC 7807. No stack traces in prod." },
          { name: "IFileStorage Abstraction", note: "Domain interface. LocalFileStorage in dev. Swap to S3FileStorage / AzureBlobStorage in prod — zero Application layer changes." },
          { name: "IEventBus Abstraction", note: "Domain interface. RabbitMqEventBus in Infrastructure. Decouples Application from RabbitMQ.Client." },
          { name: "Outbox Pattern", note: "RabbitMQ publishes written to OutboxMessages table first (same EF transaction). Worker polls and publishes. Prevents message loss on crash." },
          { name: "Repository Pattern", note: "IRepository<T> per aggregate root. No generic repo exposed to Application layer." },
        ]
      }
    }
  },
  {
    id: "actors",
    icon: "👤",
    title: "Actors & Roles",
    color: "#E91E8C",
    content: {
      type: "actors",
      data: [
        { role: "Guest", badge: "Unauthenticated", badgeColor: "#555", permissions: ["Browse public catalog", "View artist / album pages", "30s HLS preview stream", "Register / Login"] },
        { role: "Listener", badge: "Authenticated", badgeColor: "#1DB954", permissions: ["Full HLS track streaming", "Create & manage playlists", "Like tracks / albums / playlists", "Follow artists & users", "View listening history", "Search music"] },
        { role: "Artist", badge: "Creator", badgeColor: "#8B5CF6", permissions: ["Upload raw audio (MP3/AAC/FLAC)", "Manage tracks & albums", "Manage artist profile", "View stream analytics", "All Listener permissions"] },
        { role: "Admin", badge: "Platform", badgeColor: "#EF4444", permissions: ["Manage all users & artists", "Approve / reject artist accounts", "Content moderation", "Manage genres & tags", "View audit logs (Serilog)", "Platform analytics"] }
      ]
    }
  },
  {
    id: "functional",
    icon: "⚙️",
    title: "Functional Requirements",
    color: "#3B82F6",
    content: {
      type: "functional",
      data: [
        {
          module: "Authentication & Security",
          icon: "🔐",
          reqs: [
            "FR-AUTH-01: Register with email + BCrypt-hashed password",
            "FR-AUTH-02: Login returns JWT access token (15 min) + refresh token (7 days, hashed in MSSQL)",
            "FR-AUTH-03: Refresh token rotation — new token on every use, old revoked in DB",
            "FR-AUTH-04: Role claims in JWT (Listener, Artist, Admin)",
            "FR-AUTH-05: Access token blacklist in Redis (short TTL matching JWT expiry)",
            "FR-AUTH-06: Account lockout after 5 failed attempts (Redis counter, TTL reset on success)",
            "FR-AUTH-07: Email verification token on registration (single-use, expires 24h)",
            "FR-AUTH-08: Password reset via signed token (hashed in DB, expires 1h)",
          ]
        },
        {
          module: "Track Upload & Processing",
          icon: "📤",
          reqs: [
            "FR-UPLOAD-01: Artist submits multipart/form-data (audio file + track metadata)",
            "FR-UPLOAD-02: API streams file to IFileStorage → saves to raw/ directory; returns rawFilePath",
            "FR-UPLOAD-03: Track record saved to MSSQL with Status = Pending, RawFilePath set",
            "FR-UPLOAD-04: OutboxMessage written (same EF transaction) — TrackUploaded event",
            "FR-UPLOAD-05: OutboxProcessor Worker publishes to RabbitMQ; marks message Processed",
            "FR-UPLOAD-06: catalog.processing consumer receives event, reads raw file, runs FFmpeg",
            "FR-UPLOAD-07: FFmpeg outputs .m3u8 playlist + .ts segments → saved to hls/{trackId}/ directory",
            "FR-UPLOAD-08: Consumer updates Track: Status = Ready, HlsPlaylistPath set, RawFilePath optionally cleared",
            "FR-UPLOAD-09: On FFmpeg failure: Track Status = Failed; message routed to DLQ; artist notified (Phase 2)",
            "FR-UPLOAD-10: Track is not streamable (returns 404) until Status = Ready",
          ]
        },
        {
          module: "HLS Streaming",
          icon: "📡",
          reqs: [
            "FR-HLS-01: Client fetches .m3u8 playlist via GET /stream/{trackId}/playlist.m3u8",
            "FR-HLS-02: .ts segment requests served via GET /stream/{trackId}/{segment}.ts",
            "FR-HLS-03: Dev: ASP.NET Core static file middleware serves hls/ directory",
            "FR-HLS-04: Prod: CDN serves HLS files; API returns redirect or pre-signed URL",
            "FR-HLS-05: Guest users receive a 30s preview playlist (separate FFmpeg output, preview.m3u8)",
            "FR-HLS-06: Authenticated users receive full playlist; role checked on playlist request",
            "FR-HLS-07: Play event recorded on first .ts segment fetch (debounced — not per-segment)",
          ]
        },
        {
          module: "Music Catalog",
          icon: "🎼",
          reqs: [
            "FR-CAT-01: Artists have profile (stage name, bio, image, verified flag, genre tags)",
            "FR-CAT-02: Albums: cover art, release date, AlbumType enum (Album | EP | Single)",
            "FR-CAT-03: Tracks store RawFilePath, HlsPlaylistPath, Status, ISRC, explicit flag, duration",
            "FR-CAT-04: Tracks support multiple artists via TrackArtists (primary + featured)",
            "FR-CAT-05: Genres and moods are M:M tags on tracks and albums",
            "FR-CAT-06: Admin approves artist account; unapproved artists cannot upload or publish",
          ]
        },
        {
          module: "Playlists",
          icon: "📋",
          reqs: [
            "FR-PL-01: Listener can create, rename, delete playlists",
            "FR-PL-02: Visibility: Public | Private | Collaborative",
            "FR-PL-03: Track order preserved via Position column in PlaylistTracks",
            "FR-PL-04: Liked Songs system playlist auto-created on UserRegistered event",
            "FR-PL-05: Only Ready tracks can be added to playlists",
          ]
        },
        {
          module: "Playback & History",
          icon: "▶️",
          reqs: [
            "FR-PLAY-01: Play event recorded (userId, trackId, timestamp, device, context)",
            "FR-PLAY-02: Play count incremented in Redis INCR; flushed to MSSQL every 5 min (Worker)",
            "FR-PLAY-03: PlaybackRecorded event → analytics consumer updates artist/track stats",
            "FR-PLAY-04: Playback position saved per user/track in Redis (resume support)",
          ]
        },
        {
          module: "Search & Discovery",
          icon: "🔍",
          reqs: [
            "FR-SEARCH-01: Full-text search over tracks, artists, albums, playlists (ElasticSearch)",
            "FR-SEARCH-02: Filters: genre, mood, year range, explicit, duration",
            "FR-SEARCH-03: Autocomplete on track/artist name (ES prefix, cached Redis)",
            "FR-SEARCH-04: Semantic 'sounds like' search via Qdrant vector similarity",
            "FR-SEARCH-05: ES and Qdrant indices populated async via TrackUploaded RabbitMQ consumers",
            "FR-SEARCH-06: Only Ready tracks are indexed",
          ]
        },
      ]
    }
  },
  {
    id: "events",
    icon: "🐇",
    title: "RabbitMQ Events",
    color: "#A855F7",
    content: {
      type: "events",
      data: {
        topology: {
          exchange: "soundwave.events (topic exchange)",
          pattern: "catalog.<entity>.<verb>  |  streaming.<verb>  |  identity.<verb>",
          dlq: "Each queue has a .dlq sibling for failed messages",
          client: "RabbitMQ.Client (raw) — IConnection pooled, IModel per operation, IEventBus abstraction"
        },
        events: [
          {
            event: "TrackUploaded",
            routingKey: "catalog.track.uploaded",
            producer: "UploadTrackCommandHandler (via OutboxProcessor)",
            consumers: [
              { queue: "catalog.processing", action: "FFmpeg: raw file → .m3u8 + .ts segments → update Track status to Ready" },
              { queue: "catalog.search", action: "Index track into ElasticSearch (fires after status = Ready)" },
              { queue: "catalog.embeddings", action: "Generate vector embedding → upsert Qdrant" },
            ],
            payload: ["TrackId", "Title", "ArtistIds[]", "AlbumId", "GenreIds[]", "RawFilePath", "DurationMs", "IsExplicit", "OccurredAt"]
          },
          {
            event: "PlaybackRecorded",
            routingKey: "streaming.playback.recorded",
            producer: "RecordPlayCommandHandler (via OutboxProcessor)",
            consumers: [
              { queue: "analytics.playback", action: "Update artist/track stream counts in analytics store" },
              { queue: "recommendations.history", action: "Update user listening history for recommendations" },
            ],
            payload: ["UserId", "TrackId", "DurationPlayedMs", "DeviceType", "ContextType", "ContextId", "OccurredAt"]
          },
          {
            event: "UserRegistered",
            routingKey: "identity.user.registered",
            producer: "RegisterCommandHandler (via OutboxProcessor)",
            consumers: [
              { queue: "identity.email", action: "Send verification email" },
              { queue: "identity.setup", action: "Create Liked Songs system playlist for user" },
            ],
            payload: ["UserId", "Email", "DisplayName", "OccurredAt"]
          },
          {
            event: "ArtistApproved",
            routingKey: "identity.artist.approved",
            producer: "ApproveArtistCommandHandler (via OutboxProcessor)",
            consumers: [
              { queue: "identity.email", action: "Notify artist of approval" },
              { queue: "catalog.artist.index", action: "Index artist profile into ElasticSearch" },
            ],
            payload: ["ArtistId", "UserId", "ApprovedBy", "OccurredAt"]
          },
        ]
      }
    }
  },
  {
    id: "schema",
    icon: "🗄️",
    title: "DB Schema (MSSQL)",
    color: "#8B5CF6",
    content: {
      type: "schema",
      data: {
        tables: [
          { name: "Users", cols: ["Id (uniqueidentifier PK)", "Email (nvarchar unique)", "PasswordHash (nvarchar)", "DisplayName (nvarchar)", "AvatarUrl (nvarchar)", "Role (tinyint)", "IsEmailVerified (bit)", "IsLocked (bit)", "CreatedAt (datetime2)", "UpdatedAt (datetime2)"], relations: ["→ Playlists", "→ UserFollows", "→ PlayHistory", "→ LikedTracks", "→ RefreshTokens"] },
          { name: "Artists", cols: ["Id (uniqueidentifier PK)", "UserId (FK → Users)", "StageName (nvarchar)", "Bio (nvarchar)", "ProfileImageUrl (nvarchar)", "IsVerified (bit)", "IsApproved (bit)", "MonthlyListeners (int)", "CreatedAt (datetime2)"], relations: ["→ Albums", "→ TrackArtists", "→ ArtistFollows"] },
          { name: "Albums", cols: ["Id (uniqueidentifier PK)", "Title (nvarchar)", "CoverArtUrl (nvarchar)", "ReleaseDate (date)", "AlbumType (tinyint: 0=Album 1=EP 2=Single)", "Label (nvarchar)", "IsPublished (bit)", "CreatedAt (datetime2)"], relations: ["→ Tracks", "→ AlbumArtists", "→ AlbumGenres"] },
          { name: "Tracks", cols: ["Id (uniqueidentifier PK)", "AlbumId (FK → Albums)", "Title (nvarchar)", "DurationMs (int)", "TrackNumber (tinyint)", "RawFilePath (nvarchar null)", "HlsPlaylistPath (nvarchar null)", "PreviewPlaylistPath (nvarchar null)", "Status (tinyint: 0=Pending 1=Processing 2=Ready 3=Failed)", "Isrc (varchar 12)", "IsExplicit (bit)", "PlayCount (bigint)", "CreatedAt (datetime2)"], relations: ["→ TrackArtists", "→ TrackGenres", "→ PlaylistTracks", "→ PlayHistory"] },
          { name: "Playlists", cols: ["Id (uniqueidentifier PK)", "OwnerId (FK → Users)", "Title (nvarchar)", "Description (nvarchar)", "CoverArtUrl (nvarchar)", "Visibility (tinyint: 0=Private 1=Public 2=Collaborative)", "IsSystem (bit)", "FollowerCount (int)", "CreatedAt (datetime2)"], relations: ["→ PlaylistTracks", "→ PlaylistCollaborators"] },
          { name: "PlayHistory", cols: ["Id (bigint PK IDENTITY)", "UserId (FK)", "TrackId (FK)", "PlayedAt (datetime2)", "DurationPlayedMs (int)", "DeviceType (tinyint)", "ContextType (tinyint)", "ContextId (uniqueidentifier)"], relations: [] },
          { name: "RefreshTokens", cols: ["Id (uniqueidentifier PK)", "UserId (FK)", "TokenHash (nvarchar)", "ExpiresAt (datetime2)", "CreatedAt (datetime2)", "RevokedAt (datetime2 null)", "DeviceInfo (nvarchar)"], relations: [] },
          { name: "OutboxMessages", cols: ["Id (uniqueidentifier PK)", "OccurredAt (datetime2)", "Type (nvarchar)", "Payload (nvarchar MAX)", "ProcessedAt (datetime2 null)", "Error (nvarchar null)"], relations: [] },
          { name: "Genres / Moods", cols: ["Id (int PK)", "Name (nvarchar)", "Slug (varchar unique)", "Type (tinyint: 0=Genre 1=Mood)", "ColorHex (varchar 7)"], relations: ["↔ Tracks (M:M)", "↔ Albums (M:M)"] },
        ],
        junctions: [
          "AlbumArtists (AlbumId, ArtistId, IsPrimary bit)",
          "TrackArtists (TrackId, ArtistId, Role tinyint)",
          "PlaylistTracks (PlaylistId, TrackId, Position int, AddedBy FK, AddedAt)",
          "LikedTracks (UserId, TrackId, LikedAt) — composite PK",
          "LikedAlbums (UserId, AlbumId, LikedAt) — composite PK",
          "ArtistFollows (UserId, ArtistId, FollowedAt) — composite PK",
          "UserFollows (FollowerId, FolloweeId, CreatedAt) — composite PK",
          "PlaylistCollaborators (PlaylistId, UserId, PermissionLevel tinyint)",
          "TrackGenres (TrackId, GenreId) — composite PK",
          "AlbumGenres (AlbumId, GenreId) — composite PK",
        ]
      }
    }
  },
  {
    id: "filestorage",
    icon: "💾",
    title: "File Storage",
    color: "#10B981",
    content: { type: "filestorage" }
  },
  {
    id: "tech",
    icon: "🛠️",
    title: "Tech Stack",
    color: "#1DB954",
    content: {
      type: "tech",
      data: {
        "Backend (.NET 8)": [
          { name: "ASP.NET Core 8", purpose: "Web API host" },
          { name: "MediatR", purpose: "CQRS dispatch" },
          { name: "FluentValidation", purpose: "Command validation pipeline" },
          { name: "EF Core 8", purpose: "ORM + migrations" },
          { name: "MSSQL", purpose: "Primary relational DB" },
          { name: "StackExchange.Redis", purpose: "Cache, rate limit, counters" },
          { name: "RabbitMQ.Client", purpose: "Raw event bus (no MassTransit)" },
          { name: "FFmpeg (CLI)", purpose: "Audio → HLS transcoding (invoked via Process)" },
          { name: "Serilog", purpose: "Structured logging (file + Seq)" },
          { name: "xUnit + Moq", purpose: "Unit + integration tests" },
          { name: "BCrypt.Net", purpose: "Password hashing" },
          { name: ".NET Worker Service", purpose: "Outbox processor, play count flush" },
        ],
        "Frontend (React)": [
          { name: "React + Vite", purpose: "SPA framework" },
          { name: "React Router v6", purpose: "Client routing" },
          { name: "Context + useReducer", purpose: "Global player + auth state (no Zustand/Redux)" },
          { name: "TanStack Query", purpose: "Server state + caching" },
          { name: "hls.js", purpose: "HLS playback in browser (Safari uses native fallback)" },
          { name: "Tailwind CSS", purpose: "Styling" },
          { name: "Axios", purpose: "HTTP client with JWT interceptor + auto-refresh" },
        ],
        "Architecture & Patterns": [
          { name: "Modular Monolith", purpose: "Single deployment, 7 MSSQL schemas (Identity, Catalog, Streaming, Playlist, Social, Analytics, Infra)" },
          { name: "Vertical Slice Architecture", purpose: "Features own everything top to bottom — one folder per use case, not one folder per layer" },
          { name: "CQRS + MediatR", purpose: "Commands mutate state, queries use AsNoTracking() + return flat DTOs. Level 1 — same DB, no separate read store" },
          { name: "Clean Architecture", purpose: "Domain → Application → Infrastructure → Presentation. Domain has zero dependencies" },
          { name: "Result<T> Pattern", purpose: "No exception-driven flow in business logic. All handlers return Result<T> or Error" },
          { name: "Outbox Pattern", purpose: "RabbitMQ events written to Infra.OutboxMessages in same EF transaction. Worker publishes async" },
          { name: "Repository Pattern", purpose: "IRepository<T> per aggregate root. Mocked in unit tests. EF Core hidden behind interface" },
          { name: "Soft Delete", purpose: "EF Core HasQueryFilter(e => !e.IsDeleted) on all entities. Admin can bypass with IgnoreQueryFilters()" },
        ],
        "ID Strategy": [
          { name: "Guid.CreateVersion7()", purpose: "All main entities — sequential GUIDs, no index fragmentation, generated in app code before DB insert" },
          { name: "BIGINT IDENTITY", purpose: "Append-only high-volume tables only: Streaming.PlayHistory, Infra.AuditLogs" },
          { name: "No cross-schema FKs", purpose: "Module independence — plain Guid properties, no HasForeignKey(), app layer enforces consistency" },
          { name: "ValueGeneratedNever()", purpose: "EF Core config — app always supplies the ID, DB never generates it" },
        ],
        "Search (Phase 5)": [
          { name: "ElasticSearch 8", purpose: "Full-text, filters, autocomplete" },
          { name: "Elastic.Clients.Elasticsearch", purpose: ".NET ES client" },
          { name: "Qdrant", purpose: "Vector/semantic search" },
          { name: "Qdrant .NET SDK", purpose: "Upsert + nearest-neighbor queries" },
        ],
        "Infrastructure / Dev": [
          { name: "Docker Compose", purpose: "MSSQL, Redis, RabbitMQ, ES, Qdrant, Seq" },
          { name: "RabbitMQ Management UI", purpose: "Queue monitor (port 15672)" },
          { name: "Seq", purpose: "Serilog structured log viewer" },
          { name: "Local Disk", purpose: "Dev file storage (/uploads/raw, /uploads/hls)" },
          { name: "GitHub Actions", purpose: "CI/CD" },
        ]
      }
    }
  },
  {
    id: "nonfunctional",
    icon: "📊",
    title: "Non-Functional",
    color: "#F59E0B",
    content: {
      type: "nonfunctional",
      data: [
        { category: "Observability", icon: "📡", items: ["Serilog JSON logs to file + Seq sink", "Correlation ID middleware — propagated to RabbitMQ message headers", "Health checks: /health (liveness), /health/ready (DB + Redis + RabbitMQ)", "HLS consumer logs FFmpeg exit code, duration, output path per track"] },
        { category: "Testing (xUnit)", icon: "🧪", items: ["Handlers tested in isolation — Moq for IFileStorage, IEventBus, IRepository", "IFileStorage mock: verify SaveAsync called with correct path/content", "Integration tests: Testcontainers (MSSQL) for repo layer", "FluentValidation: dedicated test class per command validator", "Result<T> assertions — never assert on exceptions", "80%+ coverage target on Application layer"] },
        { category: "Security", icon: "🛡️", items: ["JWT validated on every protected endpoint", "Refresh token rotation + DB revocation", "Redis sliding window rate limiting per IP", "File upload: validate MIME type + magic bytes (not just extension)", "Max file size enforced at middleware level (e.g. 50MB)", "HLS segments not accessible until Track.Status = Ready"] },
        { category: "Reliability", icon: "🔒", items: ["Outbox pattern: publish never happens inside command handler directly", "Dead-letter queues on all RabbitMQ consumers", "Track stuck in Processing state: background job retries or marks Failed after timeout", "Play count: Redis INCR → MSSQL flush every 5 min (Worker)"] },
        { category: "Performance", icon: "⚡", items: ["HLS adaptive bitrate: FFmpeg outputs multiple renditions (128k, 256k) — Phase 2", "Hot catalog data cached in Redis (top charts, genre lists)", "EF Core: no N+1, use Include/split queries", "Search via ElasticSearch — never MSSQL LIKE", "Static HLS files served directly, API not in segment delivery path"] },
      ]
    }
  },
  {
    id: "risks",
    icon: "⚠️",
    title: "Decisions & Risks",
    color: "#F97316",
    content: {
      type: "risks",
      data: [
        { risk: "IFileStorage abstraction", level: "Decision", color: "#10B981", notes: "Define IFileStorage in Domain: Task<string> SaveAsync(Stream, string path), Task<Stream> ReadAsync(string path), Task DeleteAsync(string path). LocalFileStorage in Infrastructure for dev. Swap to S3FileStorage or AzureBlobStorage in prod — Application layer is untouched." },
        { risk: "HLS generation via FFmpeg", level: "Architecture", color: "#8B5CF6", notes: "catalog.processing consumer invokes FFmpeg CLI via System.Diagnostics.Process. Output: hls/{trackId}/playlist.m3u8 + segment_0.ts, segment_1.ts etc. Also generate preview.m3u8 (first 30s). Update Track.Status = Ready + HlsPlaylistPath on success, Failed on non-zero exit." },
        { risk: "Raw file retention policy", level: "Decision", color: "#3B82F6", notes: "After HLS is generated and verified, raw file can be deleted to save disk space. Make this configurable (DeleteRawAfterProcessing flag). In dev, keep both. In prod, delete raw after consumer confirms segments uploaded successfully." },
        { risk: "Outbox → RabbitMQ", level: "Architecture", color: "#8B5CF6", notes: "Write OutboxMessage in same EF SaveChanges as Track insert. OutboxProcessor Worker polls every 5s, publishes to RabbitMQ, marks ProcessedAt. Prevents lost events if API crashes between DB write and publish. No Hangfire needed for this — plain Worker Service." },
        { risk: "Track not streamable until Ready", level: "Decision", color: "#3B82F6", notes: "GET /stream/{trackId}/playlist.m3u8 returns 404 if Track.Status != Ready. Frontend polls track status endpoint (or uses SignalR in Phase 4) to know when to enable playback UI. Never serve a partial/broken HLS playlist." },
        { risk: "ES + Qdrant indexing timing", level: "Medium", color: "#F59E0B", notes: "catalog.search and catalog.embeddings consumers should only index after Track.Status = Ready. Options: (1) check status before indexing in consumer, (2) emit a separate TrackReady event from the processing consumer. Option 2 is cleaner — processing consumer becomes a producer too." },
        { risk: "Local disk in dev", level: "Low", color: "#1DB954", notes: "Store files at wwwroot/uploads/raw/{trackId}.{ext} and wwwroot/uploads/hls/{trackId}/. ASP.NET Core static files middleware serves them. Ensure .gitignore covers uploads/. Replace with IFileStorage cloud impl before any deployment." },
      ]
    }
  },
  {
    id: "outofscope",
    icon: "🚫",
    title: "Out of Scope (v1)",
    color: "#EF4444",
    content: {
      type: "list",
      data: [
        "HLS adaptive bitrate (multiple renditions) — Phase 2",
        "Cloud file storage (S3 / Azure Blob) — replace IFileStorage impl post-dev",
        "CDN for HLS segment delivery — prod concern",
        "Subscription / payment tiers",
        "Native mobile apps",
        "Live streaming / radio",
        "Podcast hosting",
        "Two-factor authentication",
        "OAuth2 social login",
        "Real-time notifications (SignalR — Phase 4)",
        "Lyrics sync / display",
      ]
    }
  },
];

// ── Upload Pipeline Diagram ───────────────────────────────────────────────────
const UploadPipeline = () => {
  const steps = [
    { id: 1, label: "Artist", sub: "multipart/form-data\n(audio + metadata)", color: "#8B5CF6", icon: "🎤" },
    { id: 2, label: "API", sub: "UploadTrackCommand\nvia MediatR", color: "#3B82F6", icon: "🌐" },
    { id: 3, label: "IFileStorage", sub: "LocalFileStorage (dev)\nraw/{trackId}.mp3", color: "#10B981", icon: "💾" },
    { id: 4, label: "MSSQL", sub: "Track inserted\nStatus = Pending", color: "#F59E0B", icon: "🗄️" },
    { id: 5, label: "Outbox", sub: "OutboxMessage row\n(same transaction)", color: "#EC4899", icon: "📬" },
    { id: 6, label: "RabbitMQ", sub: "catalog.track.uploaded\n(OutboxProcessor)", color: "#F97316", icon: "🐇" },
    { id: 7, label: "FFmpeg", sub: "catalog.processing\nconsumer", color: "#EF4444", icon: "⚙️" },
    { id: 8, label: "HLS Output", sub: "hls/{trackId}/\nplaylist.m3u8 + *.ts", color: "#10B981", icon: "📡" },
    { id: 9, label: "Track Ready", sub: "Status = Ready\nHlsPlaylistPath set", color: "#1DB954", icon: "✅" },
  ];

  const sideConsumers = [
    { label: "ES Index", sub: "catalog.search", color: "#06B6D4", icon: "🔍" },
    { label: "Qdrant", sub: "catalog.embeddings", color: "#A855F7", icon: "🧠" },
  ];

  return (
    <div>
      <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#F97316", marginBottom: 20, textTransform: "uppercase" }}>End-to-End Upload & HLS Processing Flow</div>

      {/* Main flow */}
      <div style={{ display: "flex", alignItems: "center", flexWrap: "wrap", gap: 0, marginBottom: 32 }}>
        {steps.map((s, i) => (
          <div key={s.id} style={{ display: "flex", alignItems: "center" }}>
            <div style={{ display: "flex", flexDirection: "column", alignItems: "center", width: 90 }}>
              <div style={{ width: 48, height: 48, borderRadius: 12, background: s.color + "22", border: `2px solid ${s.color}66`, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 22, marginBottom: 6 }}>{s.icon}</div>
              <div style={{ fontSize: 12, fontWeight: 700, color: "#fff", textAlign: "center" }}>{s.label}</div>
              <div style={{ fontSize: 10, color: "#666", textAlign: "center", lineHeight: 1.4, marginTop: 3, whiteSpace: "pre-line" }}>{s.sub}</div>
            </div>
            {i < steps.length - 1 && (
              <div style={{ color: "#333", fontSize: 18, margin: "0 2px", marginBottom: 24 }}>→</div>
            )}
          </div>
        ))}
      </div>

      {/* Fork from RabbitMQ */}
      <div style={{ background: "#111", border: "1px solid #2a2a2a", borderRadius: 12, padding: 20 }}>
        <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#F97316", marginBottom: 12, textTransform: "uppercase" }}>RabbitMQ Fan-out — TrackUploaded consumers</div>
        <div style={{ display: "flex", gap: 12, flexWrap: "wrap" }}>
          {[
            { queue: "catalog.processing", action: "FFmpeg → HLS → update Track.Status", color: "#EF4444", phase: "Phase 1" },
            { queue: "catalog.search", action: "Index in ElasticSearch (after Ready)", color: "#06B6D4", phase: "Phase 3" },
            { queue: "catalog.embeddings", action: "Embed → upsert Qdrant (after Ready)", color: "#A855F7", phase: "Phase 3" },
          ].map(c => (
            <div key={c.queue} style={{ flex: 1, minWidth: 200, background: "#0d0d0d", border: `1px solid ${c.color}44`, borderRadius: 10, padding: "12px 16px" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 6 }}>
                <code style={{ color: c.color, fontSize: 12 }}>{c.queue}</code>
                <span style={{ fontSize: 10, color: "#555", background: "#1a1a1a", padding: "2px 6px", borderRadius: 4 }}>{c.phase}</span>
              </div>
              <div style={{ fontSize: 12, color: "#aaa" }}>{c.action}</div>
              <div style={{ fontSize: 11, color: "#444", marginTop: 6 }}>DLQ: {c.queue}.dlq</div>
            </div>
          ))}
        </div>
      </div>

      {/* Status state machine */}
      <div style={{ marginTop: 20, background: "#111", border: "1px solid #2a2a2a", borderRadius: 12, padding: 20 }}>
        <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#8B5CF6", marginBottom: 12, textTransform: "uppercase" }}>Track.Status State Machine</div>
        <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
          {[
            { s: "Pending", c: "#F59E0B", note: "DB insert" },
            { s: "Processing", c: "#3B82F6", note: "FFmpeg running" },
            { s: "Ready", c: "#1DB954", note: "HLS available" },
            { s: "Failed", c: "#EF4444", note: "FFmpeg error → DLQ" },
          ].map((item, i, arr) => (
            <div key={item.s} style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <div style={{ textAlign: "center" }}>
                <div style={{ background: item.c + "22", border: `1px solid ${item.c}66`, borderRadius: 8, padding: "6px 14px", color: item.c, fontWeight: 700, fontSize: 13 }}>{item.s}</div>
                <div style={{ fontSize: 10, color: "#555", marginTop: 3 }}>{item.note}</div>
              </div>
              {i < arr.length - 2 && <span style={{ color: "#333", fontSize: 16 }}>→</span>}
              {i === arr.length - 2 && <span style={{ color: "#333", fontSize: 16 }}>→</span>}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

// ── File Storage Section ──────────────────────────────────────────────────────
const FileStorage = () => {
  const structure = [
    { path: "wwwroot/", note: "ASP.NET Core static files root", depth: 0 },
    { path: "uploads/", note: "", depth: 1 },
    { path: "raw/", note: "Uploaded audio before processing", depth: 2 },
    { path: "{trackId}.mp3", note: "Original file, any format", depth: 3 },
    { path: "hls/", note: "FFmpeg output per track", depth: 2 },
    { path: "{trackId}/", note: "One dir per track", depth: 3 },
    { path: "playlist.m3u8", note: "Full track HLS manifest", depth: 4 },
    { path: "preview.m3u8", note: "30s preview manifest", depth: 4 },
    { path: "segment_000.ts", note: "MPEG-TS segments", depth: 4 },
    { path: "segment_001.ts", note: "", depth: 4 },
    { path: "...", note: "", depth: 4 },
  ];

  const iface = [
    { sig: "Task<string> SaveAsync(Stream stream, string relativePath)", note: "Saves file, returns stored path" },
    { sig: "Task<Stream> ReadAsync(string relativePath)", note: "Opens stream for reading" },
    { sig: "Task DeleteAsync(string relativePath)", note: "Deletes file (post-processing cleanup)" },
    { sig: "string GetUrl(string relativePath)", note: "Returns serving URL (local: /uploads/..., prod: CDN URL)" },
  ];

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
      <div style={{ background: "rgba(16,185,129,0.08)", border: "1px solid rgba(16,185,129,0.25)", borderRadius: 12, padding: "16px 20px" }}>
        <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#10B981", marginBottom: 8, textTransform: "uppercase" }}>Strategy</div>
        <div style={{ fontSize: 14, color: "#ccc", lineHeight: 1.7 }}>
          Define <code style={{ color: "#10B981", background: "#0d0d0d", padding: "1px 6px", borderRadius: 4 }}>IFileStorage</code> in the <strong style={{ color: "#fff" }}>Domain layer</strong>. 
          Implement <code style={{ color: "#10B981", background: "#0d0d0d", padding: "1px 6px", borderRadius: 4 }}>LocalFileStorage</code> in Infrastructure for dev. 
          Swap to <code style={{ color: "#3B82F6", background: "#0d0d0d", padding: "1px 6px", borderRadius: 4 }}>S3FileStorage</code> or <code style={{ color: "#3B82F6", background: "#0d0d0d", padding: "1px 6px", borderRadius: 4 }}>AzureBlobStorage</code> in prod via DI — <strong style={{ color: "#fff" }}>zero Application layer changes</strong>.
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
        <div>
          <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#888", marginBottom: 10, textTransform: "uppercase" }}>Directory Structure (Dev)</div>
          <div style={{ background: "#0d0d0d", border: "1px solid #1a1a1a", borderRadius: 10, padding: 16, fontFamily: "monospace" }}>
            {structure.map((item, i) => (
              <div key={i} style={{ display: "flex", alignItems: "flex-start", gap: 8, paddingLeft: item.depth * 16, marginBottom: 3 }}>
                <span style={{ color: item.path.endsWith("/") ? "#F59E0B" : item.path === "..." ? "#444" : "#ccc", fontSize: 12 }}>
                  {item.path.endsWith("/") ? "📁 " : item.path === "..." ? "   " : "📄 "}{item.path}
                </span>
                {item.note && <span style={{ color: "#444", fontSize: 11, marginTop: 1 }}>← {item.note}</span>}
              </div>
            ))}
          </div>
        </div>

        <div>
          <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#888", marginBottom: 10, textTransform: "uppercase" }}>IFileStorage Interface</div>
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {iface.map(m => (
              <div key={m.sig} style={{ background: "#0d0d0d", border: "1px solid #1a1a1a", borderRadius: 8, padding: "10px 14px" }}>
                <code style={{ color: "#10B981", fontSize: 11, display: "block", marginBottom: 4, wordBreak: "break-all" }}>{m.sig}</code>
                <div style={{ color: "#555", fontSize: 11 }}>{m.note}</div>
              </div>
            ))}
          </div>
          <div style={{ marginTop: 12, background: "#0d0d0d", border: "1px solid #2a2a2a", borderRadius: 8, padding: "12px 14px" }}>
            <div style={{ fontSize: 11, color: "#666", marginBottom: 6, textTransform: "uppercase", letterSpacing: 2 }}>Implementations</div>
            {[
              { name: "LocalFileStorage", env: "dev", color: "#10B981" },
              { name: "S3FileStorage", env: "prod", color: "#F59E0B" },
              { name: "AzureBlobStorage", env: "prod", color: "#3B82F6" },
            ].map(impl => (
              <div key={impl.name} style={{ display: "flex", justifyContent: "space-between", marginBottom: 4 }}>
                <code style={{ color: impl.color, fontSize: 12 }}>{impl.name}</code>
                <span style={{ fontSize: 11, color: "#555" }}>{impl.env}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div style={{ background: "#111", border: "1px solid #2a2a2a", borderRadius: 10, padding: "14px 18px" }}>
        <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 2, color: "#F59E0B", marginBottom: 8, textTransform: "uppercase" }}>HLS Serving — Dev vs Prod</div>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
          <div>
            <div style={{ fontSize: 12, color: "#888", marginBottom: 6 }}>Dev</div>
            <div style={{ fontSize: 13, color: "#ccc", lineHeight: 1.7 }}>ASP.NET Core static files middleware serves <code style={{ color: "#10B981" }}>/uploads/hls/</code> directly. No CDN needed. Register in <code style={{ color: "#10B981" }}>Program.cs</code>: <code style={{ color: "#aaa" }}>UseStaticFiles()</code>.</div>
          </div>
          <div>
            <div style={{ fontSize: 12, color: "#888", marginBottom: 6 }}>Prod</div>
            <div style={{ fontSize: 13, color: "#ccc", lineHeight: 1.7 }}>API returns a time-limited pre-signed URL (S3/Azure). <code style={{ color: "#3B82F6" }}>GetUrl()</code> in the impl generates the signed URL. Client fetches .m3u8 and .ts directly from CDN — API not in segment path.</div>
          </div>
        </div>
      </div>
    </div>
  );
};

// ── Shared components ─────────────────────────────────────────────────────────
const Overview = ({ data }) => (
  <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
    <div style={{ background: "rgba(29,185,84,0.08)", border: "1px solid rgba(29,185,84,0.25)", borderRadius: 12, padding: "20px 24px" }}>
      <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#1DB954", marginBottom: 6, textTransform: "uppercase" }}>Application</div>
      <div style={{ fontSize: 24, fontWeight: 800, color: "#fff", marginBottom: 8 }}>{data.name}</div>
      <div style={{ color: "#aaa", lineHeight: 1.7, fontSize: 14 }}>{data.goal}</div>
    </div>
    <div>
      <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#888", marginBottom: 10, textTransform: "uppercase" }}>Stack</div>
      <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
        {data.stack.map(s => <span key={s} style={{ background: "#1a1a1a", border: "1px solid #333", borderRadius: 6, padding: "5px 12px", fontSize: 13, color: "#ddd", fontFamily: "monospace" }}>{s}</span>)}
      </div>
    </div>
    <div>
      <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#888", marginBottom: 10, textTransform: "uppercase" }}>Delivery Phases</div>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
        {data.phases.map((p, i) => (
          <div key={i} style={{ background: "#111", border: "1px solid #2a2a2a", borderRadius: 10, padding: "14px 16px" }}>
            <div style={{ fontSize: 12, fontWeight: 700, color: "#1DB954", marginBottom: 8 }}>{p.label}</div>
            {p.items.map(item => <div key={item} style={{ fontSize: 12, color: "#aaa", paddingLeft: 8, borderLeft: "2px solid #222", marginBottom: 4 }}>{item}</div>)}
          </div>
        ))}
      </div>
    </div>
  </div>
);

const Architecture = ({ data }) => {
  const [tab, setTab] = useState("layers");
  return (
    <div>
      <div style={{ display: "flex", gap: 4, marginBottom: 20 }}>
        {["layers", "patterns"].map(t => (
          <button key={t} onClick={() => setTab(t)} style={{ padding: "7px 18px", borderRadius: 8, border: "none", background: tab === t ? "#06B6D4" : "#1a1a1a", color: tab === t ? "#000" : "#888", fontWeight: 700, fontSize: 13, cursor: "pointer", textTransform: "capitalize" }}>{t}</button>
        ))}
      </div>
      {tab === "layers" && (
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          {data.layers.map(l => (
            <div key={l.name} style={{ background: "#111", border: `1px solid ${l.color}33`, borderRadius: 10, padding: "16px 20px" }}>
              <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 10 }}>
                <span style={{ fontWeight: 800, fontSize: 16, color: l.color }}>{l.name}</span>
                <span style={{ color: "#666", fontSize: 13 }}>{l.desc}</span>
              </div>
              <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
                {l.contents.map(c => <span key={c} style={{ background: "#0d0d0d", border: "1px solid #222", borderRadius: 6, padding: "4px 10px", fontSize: 12, color: "#bbb", fontFamily: "monospace" }}>{c}</span>)}
              </div>
            </div>
          ))}
        </div>
      )}
      {tab === "patterns" && (
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          {data.patterns.map(p => (
            <div key={p.name} style={{ background: "#111", border: "1px solid #2a2a2a", borderRadius: 10, padding: "14px 18px", display: "flex", gap: 16, alignItems: "flex-start" }}>
              <span style={{ fontWeight: 700, color: "#06B6D4", fontSize: 14, whiteSpace: "nowrap", minWidth: 220 }}>{p.name}</span>
              <span style={{ color: "#aaa", fontSize: 13, lineHeight: 1.6 }}>{p.note}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

const Actors = ({ data }) => (
  <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
    {data.map(a => (
      <div key={a.role} style={{ background: "#111", border: "1px solid #2a2a2a", borderRadius: 10, padding: "16px 20px" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 12 }}>
          <span style={{ fontSize: 18, fontWeight: 800, color: "#fff" }}>{a.role}</span>
          <span style={{ background: a.badgeColor + "22", color: a.badgeColor, border: `1px solid ${a.badgeColor}55`, borderRadius: 20, padding: "2px 10px", fontSize: 11, fontWeight: 700 }}>{a.badge}</span>
        </div>
        <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
          {a.permissions.map(p => <span key={p} style={{ background: "#1a1a1a", border: "1px solid #2a2a2a", borderRadius: 6, padding: "4px 10px", fontSize: 12, color: "#ccc" }}>✓ {p}</span>)}
        </div>
      </div>
    ))}
  </div>
);

const Functional = ({ data }) => {
  const [open, setOpen] = useState(null);
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      {data.map((mod, i) => (
        <div key={mod.module} style={{ background: "#111", border: `1px solid ${open === i ? "#3B82F6" : "#2a2a2a"}`, borderRadius: 10, overflow: "hidden" }}>
          <button onClick={() => setOpen(open === i ? null : i)} style={{ width: "100%", display: "flex", alignItems: "center", justifyContent: "space-between", padding: "14px 20px", background: "none", border: "none", cursor: "pointer", color: "#fff" }}>
            <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <span style={{ fontSize: 20 }}>{mod.icon}</span>
              <span style={{ fontWeight: 700, fontSize: 15 }}>{mod.module}</span>
              <span style={{ background: "#1a1a1a", borderRadius: 20, padding: "2px 8px", fontSize: 11, color: "#888" }}>{mod.reqs.length} reqs</span>
            </div>
            <span style={{ color: "#888", transform: open === i ? "rotate(180deg)" : "none", transition: "transform 0.2s" }}>▼</span>
          </button>
          {open === i && (
            <div style={{ padding: "0 20px 16px", display: "flex", flexDirection: "column", gap: 6 }}>
              {mod.reqs.map(r => (
                <div key={r} style={{ display: "flex", gap: 10, alignItems: "flex-start" }}>
                  <span style={{ color: "#3B82F6", fontFamily: "monospace", fontSize: 11, marginTop: 2, whiteSpace: "nowrap" }}>{r.split(":")[0]}</span>
                  <span style={{ color: "#ccc", fontSize: 13 }}>{r.split(":").slice(1).join(":").trim()}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      ))}
    </div>
  );
};

const Events = ({ data }) => {
  const [sel, setSel] = useState(0);
  const ev = data.events[sel];
  return (
    <div>
      <div style={{ background: "rgba(168,85,247,0.08)", border: "1px solid rgba(168,85,247,0.25)", borderRadius: 10, padding: "14px 18px", marginBottom: 20 }}>
        <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 2, color: "#A855F7", marginBottom: 8, textTransform: "uppercase" }}>Topology</div>
        <div style={{ display: "flex", flexWrap: "wrap", gap: 20 }}>
          {Object.entries(data.topology).map(([k, v]) => (
            <div key={k}><div style={{ fontSize: 11, color: "#555", textTransform: "capitalize", marginBottom: 2 }}>{k}</div><div style={{ fontSize: 13, color: "#ddd", fontFamily: "monospace" }}>{v}</div></div>
          ))}
        </div>
      </div>
      <div style={{ display: "flex", gap: 6, marginBottom: 16, flexWrap: "wrap" }}>
        {data.events.map((e, i) => (
          <button key={e.event} onClick={() => setSel(i)} style={{ padding: "6px 14px", borderRadius: 8, border: `1px solid ${sel === i ? "#A855F7" : "#2a2a2a"}`, background: sel === i ? "rgba(168,85,247,0.12)" : "#111", color: sel === i ? "#A855F7" : "#888", fontWeight: sel === i ? 700 : 400, fontSize: 13, cursor: "pointer", fontFamily: "monospace" }}>{e.event}</button>
        ))}
      </div>
      <div style={{ background: "#111", border: "1px solid #2a2a2a", borderRadius: 10, padding: 20 }}>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, marginBottom: 16 }}>
          <div><div style={{ fontSize: 11, color: "#555", marginBottom: 4, textTransform: "uppercase", letterSpacing: 2 }}>Routing Key</div><code style={{ color: "#A855F7", fontSize: 13 }}>{ev.routingKey}</code></div>
          <div><div style={{ fontSize: 11, color: "#555", marginBottom: 4, textTransform: "uppercase", letterSpacing: 2 }}>Producer</div><code style={{ color: "#ccc", fontSize: 13 }}>{ev.producer}</code></div>
        </div>
        <div style={{ marginBottom: 16 }}>
          <div style={{ fontSize: 11, color: "#555", marginBottom: 8, textTransform: "uppercase", letterSpacing: 2 }}>Payload</div>
          <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
            {ev.payload.map(p => <span key={p} style={{ background: "#0d0d0d", border: "1px solid #222", borderRadius: 6, padding: "3px 10px", fontSize: 12, color: "#aaa", fontFamily: "monospace" }}>{p}</span>)}
          </div>
        </div>
        <div>
          <div style={{ fontSize: 11, color: "#555", marginBottom: 8, textTransform: "uppercase", letterSpacing: 2 }}>Consumers</div>
          {ev.consumers.map(c => (
            <div key={c.queue} style={{ display: "flex", gap: 12, alignItems: "center", padding: "10px 14px", background: "#0d0d0d", borderRadius: 8, border: "1px solid #1a1a1a", marginBottom: 6 }}>
              <code style={{ color: "#3B82F6", fontSize: 12, minWidth: 220 }}>{c.queue}</code>
              <span style={{ color: "#888", fontSize: 12 }}>→</span>
              <span style={{ color: "#ccc", fontSize: 13 }}>{c.action}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

const Schema = ({ data }) => {
  const [sel, setSel] = useState(0);
  const t = data.tables[sel];
  return (
    <div style={{ display: "flex", gap: 16 }}>
      <div style={{ width: 210, flexShrink: 0, display: "flex", flexDirection: "column", gap: 4 }}>
        {data.tables.map((tb, i) => (
          <button key={tb.name} onClick={() => setSel(i)} style={{ textAlign: "left", padding: "8px 12px", borderRadius: 8, border: `1px solid ${sel === i ? "#8B5CF6" : "#222"}`, background: sel === i ? "rgba(139,92,246,0.12)" : "#111", color: sel === i ? "#8B5CF6" : "#aaa", fontSize: 13, fontWeight: sel === i ? 700 : 400, cursor: "pointer", fontFamily: "monospace" }}>{tb.name}</button>
        ))}
        <div style={{ marginTop: 12, fontSize: 10, color: "#555", fontWeight: 700, letterSpacing: 2, textTransform: "uppercase", padding: "0 4px" }}>Junction Tables</div>
        {data.junctions.map(j => <div key={j} style={{ padding: "5px 10px", borderRadius: 6, background: "#0d0d0d", border: "1px solid #1a1a1a", fontSize: 11, color: "#555", fontFamily: "monospace", lineHeight: 1.4, marginBottom: 3 }}>{j}</div>)}
      </div>
      <div style={{ flex: 1, background: "#111", border: "1px solid #2a2a2a", borderRadius: 10, padding: 20 }}>
        <div style={{ fontSize: 18, fontWeight: 800, color: "#8B5CF6", marginBottom: 16, fontFamily: "monospace" }}>{t.name}</div>
        {t.cols.map(c => (
          <div key={c} style={{ display: "flex", alignItems: "center", gap: 8, padding: "6px 0", borderBottom: "1px solid #1a1a1a" }}>
            <span style={{ color: c.includes("PK") ? "#F59E0B" : c.includes("FK") ? "#3B82F6" : "#ccc", fontFamily: "monospace", fontSize: 13, minWidth: 180 }}>{c.split(" (")[0]}</span>
            <span style={{ color: "#444", fontSize: 11, fontFamily: "monospace" }}>{c.includes("(") ? `(${c.split("(").slice(1).join("(")}` : ""}</span>
          </div>
        ))}
        {t.relations.length > 0 && (
          <div style={{ marginTop: 16 }}>
            <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 2, color: "#555", marginBottom: 8, textTransform: "uppercase" }}>Relations</div>
            <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
              {t.relations.map(r => <span key={r} style={{ background: "rgba(139,92,246,0.1)", border: "1px solid rgba(139,92,246,0.3)", borderRadius: 6, padding: "4px 10px", fontSize: 12, color: "#a78bfa", fontFamily: "monospace" }}>{r}</span>)}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

const Tech = ({ data }) => (
  <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
    {Object.entries(data).map(([layer, items]) => (
      <div key={layer}>
        <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: 3, color: "#1DB954", marginBottom: 10, textTransform: "uppercase" }}>{layer}</div>
        <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
          {items.map(item => (
            <div key={item.name} style={{ background: "#111", border: "1px solid #2a2a2a", borderRadius: 8, padding: "10px 14px", minWidth: 160 }}>
              <div style={{ fontWeight: 700, color: "#fff", fontSize: 13, fontFamily: "monospace" }}>{item.name}</div>
              <div style={{ color: "#666", fontSize: 12, marginTop: 2 }}>{item.purpose}</div>
            </div>
          ))}
        </div>
      </div>
    ))}
  </div>
);

const NonFunctional = ({ data }) => (
  <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
    {data.map(cat => (
      <div key={cat.category} style={{ background: "#111", border: "1px solid #2a2a2a", borderRadius: 10, padding: "16px 20px" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 12 }}>
          <span style={{ fontSize: 18 }}>{cat.icon}</span>
          <span style={{ fontWeight: 700, color: "#fff", fontSize: 15 }}>{cat.category}</span>
        </div>
        {cat.items.map(item => <div key={item} style={{ fontSize: 13, color: "#aaa", marginBottom: 7, paddingLeft: 10, borderLeft: "2px solid #2a2a2a", lineHeight: 1.6 }}>{item}</div>)}
      </div>
    ))}
  </div>
);

const Risks = ({ data }) => (
  <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
    {data.map(r => (
      <div key={r.risk} style={{ background: "#111", border: `1px solid ${r.color}33`, borderRadius: 10, padding: "14px 18px" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 8 }}>
          <span style={{ fontWeight: 700, fontSize: 15, color: "#fff" }}>{r.risk}</span>
          <span style={{ background: r.color + "22", color: r.color, border: `1px solid ${r.color}55`, borderRadius: 20, padding: "2px 10px", fontSize: 11, fontWeight: 700 }}>{r.level}</span>
        </div>
        <div style={{ color: "#aaa", fontSize: 13, lineHeight: 1.6 }}>{r.notes}</div>
      </div>
    ))}
  </div>
);

const ListSection = ({ data }) => (
  <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
    {data.map((item, i) => (
      <div key={i} style={{ display: "flex", gap: 12, alignItems: "flex-start", padding: "10px 16px", background: "#111", border: "1px solid #2a2a2a", borderRadius: 8 }}>
        <span style={{ color: "#EF4444", fontSize: 16 }}>✕</span>
        <span style={{ color: "#ccc", fontSize: 14 }}>{item}</span>
      </div>
    ))}
  </div>
);

// ── App Shell ─────────────────────────────────────────────────────────────────
export default function App() {
  const [active, setActive] = useState("overview");
  const section = sections.find(s => s.id === active);

  const renderContent = (content) => {
    switch (content.type) {
      case "overview": return <Overview data={content.data} />;
      case "upload": return <UploadPipeline />;
      case "architecture": return <Architecture data={content.data} />;
      case "actors": return <Actors data={content.data} />;
      case "functional": return <Functional data={content.data} />;
      case "events": return <Events data={content.data} />;
      case "schema": return <Schema data={content.data} />;
      case "filestorage": return <FileStorage />;
      case "tech": return <Tech data={content.data} />;
      case "nonfunctional": return <NonFunctional data={content.data} />;
      case "risks": return <Risks data={content.data} />;
      case "list": return <ListSection data={content.data} />;
      default: return null;
    }
  };

  return (
    <div style={{ fontFamily: "'Inter', -apple-system, sans-serif", background: "#0a0a0a", minHeight: "100vh", color: "#fff", display: "flex" }}>
      <div style={{ width: 230, borderRight: "1px solid #1a1a1a", padding: "24px 12px", display: "flex", flexDirection: "column", gap: 3, flexShrink: 0, overflowY: "auto", maxHeight: "100vh" }}>
        <div style={{ padding: "0 8px 20px", borderBottom: "1px solid #1a1a1a", marginBottom: 8 }}>
          <div style={{ fontSize: 10, fontWeight: 800, letterSpacing: 3, color: "#1DB954", textTransform: "uppercase" }}>BRD v3</div>
          <div style={{ fontSize: 15, fontWeight: 800, color: "#fff", marginTop: 2 }}>SoundWave</div>
          <div style={{ fontSize: 11, color: "#555", marginTop: 2 }}>React + .NET 8 + MSSQL + HLS</div>
        </div>
        {sections.map(s => (
          <button key={s.id} onClick={() => setActive(s.id)} style={{ display: "flex", alignItems: "center", gap: 10, padding: "9px 12px", borderRadius: 8, border: "none", background: active === s.id ? `${s.color}18` : "transparent", color: active === s.id ? s.color : "#888", cursor: "pointer", fontSize: 13, fontWeight: active === s.id ? 700 : 400, textAlign: "left", transition: "all 0.15s", borderLeft: active === s.id ? `3px solid ${s.color}` : "3px solid transparent" }}>
            <span>{s.icon}</span> {s.title}
          </button>
        ))}
      </div>
      <div style={{ flex: 1, padding: 32, overflowY: "auto", maxHeight: "100vh" }}>
        <div style={{ maxWidth: 920 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 28 }}>
            <span style={{ fontSize: 28 }}>{section.icon}</span>
            <h1 style={{ fontSize: 24, fontWeight: 800, color: "#fff", margin: 0 }}>{section.title}</h1>
            <div style={{ flex: 1, height: 1, background: "#1a1a1a", marginLeft: 8 }} />
          </div>
          {renderContent(section.content)}
        </div>
      </div>
    </div>
  );
}
