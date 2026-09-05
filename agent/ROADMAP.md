# SoundWave – Project Roadmap
> Spotify Clone · React + .NET 8 · Modular Monolith · CQRS + Vertical Slicing

---

## How to Read This Roadmap

Each phase is broken into **sub-phases** tagged with the module they touch.  
Finish one sub-phase fully (working code, passing tests) before moving to the next.  
Each phase begins with a **X.0 — Study** sub-phase covering the technologies used in that phase — complete it before writing production code for that phase.

**Module tags used throughout:**
`[Identity]` `[Catalog]` `[Streaming]` `[Playlist]` `[Social]` `[Analytics]` `[SharedKernel]` `[Frontend]`

---

## ✅ DONE
- [x] **1.0 — Study: Core Patterns, Data Layer & Infrastructure**
- [x] **1.1 — Project Skeleton `[SharedKernel]`**
- [x] **1.2 — Identity Module: Registration & Login `[Identity]`**
- [x] **Plan 1: Integration Testing & Repository Refactoring `[Identity]`**
- [x] **Plan 2: TokenService & OtpService Refactoring `[Identity]`**
- [x] **1.2.5 — Refactoring: Cache Keys `[SharedKernel]`**
- [x] **1.2.6 — Roadmap Feature: Account Lockout Refinement `[Identity]`**
- [x] **1.3 — Identity Module: Password Reset `[Identity]`**
- [x] **1.4 — Catalog Module: Genres & Artists `[Catalog]`**
- [x] **1.8 — Frontend Shell: Spotify UI Seed `[Frontend]`**

---

## Phase 1 — Foundation & Core Modules
> **Goal:** Running API with auth, catalog browsing, and basic playlist management. No streaming yet.  
> **Technologies:** .NET 8, MSSQL, EF Core, MediatR, FluentValidation, Serilog, Redis (auth only), JWT  
> **File storage:** Not needed yet  
> **Frontend:** Basic React shell with routing only

---

### 1.4 — Catalog Module: Genres & Artists `[Catalog]`
**Features:** Admin creates genres/moods, Artist profile browsing  
**Tables:** `Catalog.Genres`, `Catalog.Artists`

**Read/Write Separation:** This phase introduces the CQRS read/write repo split. Command handlers inject `ICatalogRepository<T>` (write — full CRUD, change-tracked). Query handlers inject `ICatalogReadRepository<T>` (read-only — no `Add`/`Update`/`Delete`/`SaveChanges`, `NoTracking` by default). Both share the same connection string for now; swap the read context to a replica later with zero handler changes.

- [x] **Infrastructure: Read Context & Repository**
  - [x] `CatalogReadDbContext` — inherits same entity configs as `CatalogDbContext`, sets `QueryTrackingBehavior = NoTracking` globally, throws on `SaveChanges()` to prevent accidental writes
  - [x] `ICatalogReadRepository<T>` — read-only interface: `GetAll()`, `GetByID()`, `GetByCondition()`, `CheckExistsByID()`, `CheckExistsByCondition()` — no write methods
  - [x] `CatalogReadRepository<T>` — backed by `CatalogReadDbContext`
  - [x] DI registration: `ICatalogRepository<T>` → `CatalogDbContext`, `ICatalogReadRepository<T>` → `CatalogReadDbContext`
- [x] `CreateGenreCommand` `[Admin]` → insert `Catalog.Genres`
- [x] `UpdateGenreCommand` `[Admin]` → update `Catalog.Genres`
- [x] `ListGenresQuery` → list all genres/moods (Redis cached), uses `ICatalogReadRepository`
- [x] `ApplyForArtistAccountCommand` `[Listener]` → insert `Catalog.ArtistAccountApprovals` (Status=Pending), one-per-user unique constraint enforced
- [x] `ApproveArtistAccountCommand` `[Admin]` → create `Catalog.Artists` row + set approval Status=Approved + write `OutboxMessage` (ArtistApproved) + write `SharedKernel.AuditLogs` — uses `ICatalogRepository` (read-then-mutate stays on write side)
- [x] `RejectArtistAccountCommand` `[Admin]` → set approval Status=Rejected + store `RejectionReason` + write `SharedKernel.AuditLogs`
- [x] `ListArtistAccountApprovalsQuery` `[Admin]` → paginated list of approvals filtered by Status (default: Pending), uses `ICatalogReadRepository`, no cache
- [x] `GetMyArtistApplicationStatusQuery` `[Listener]` → returns the caller's own `ArtistAccountApproval` status and rejection reason if any
- [x] `GetArtistProfileQuery` → returns artist + top tracks + albums, uses `ICatalogReadRepository`
- [x] xUnit tests

---

### 1.5 — Catalog Module: Albums & Tracks `[Catalog]`
**Features:** Full release lifecycle for artists (Singles, EPs, Albums), metadata management, and catalog discovery.  
**Tables:** `Catalog.Albums`, `Catalog.Tracks`, `Catalog.TrackGenres`, `Catalog.AlbumGenres`, `Catalog.AlbumArtists`, `Catalog.TrackArtists`

#### 🎵 Release Creation Flows (Artist Studio)
- [x] `CreateSingleCommand` `[Artist]` → **1-step Single Release:** Atomically creates Single Album + Track in 1 request.
- [x] `CreateAlbumCommand` `[Artist]` → **Multi-track Release Builder:** Creates empty Album container (Album/EP) for adding tracks.
- [x] `CreateTrackCommand` `[Artist]` → **Track Builder:** Creates track within an album (auto-increments track number, links genres & collaborating artists).
- [x] `PublishAlbumCommand` `[Artist]` → **Publish Release:** Validates tracklist and sets `IsPublished = true` (makes release live to listeners).

#### 🛠️ Release Management Flows (Artist Studio)
- [x] `EditAlbumCommand` `[Artist]` → **Edit Album Form:** Bulk updates title, cover art, release date, genres, and collaborating artists.
- [x] `EditTrackMetadataCommand` `[Artist]` → **Edit Track Modal:** Bulk updates track title, genres, and replaces featured artists list.
- [x] `DeleteTrackCommand` `[Artist]` → **Remove Track:** Soft deletes track from album, decrements `TrackCount`, and re-gaps track numbers.
- [x] `MoveTrackToAlbumCommand` `[Artist]` → **Move Track:** Reassigns track to another album (preserves audio/stats, updates counts & re-gaps both albums).

#### 🎧 Discovery & Playback Flows (Public / Listener)
- [x] `GetAlbumQuery` → **Album View:** Full album details with ordered tracklist, artist credits, and genres.
- [x] `GetNewReleasesQuery` → **Home Screen Carousel:** Top recently released published albums (Redis cached).
- [x] `ListAlbumsQuery` → **Search & Browse Screen:** Paginated list with search, genre, artist, and publication filters (Redis cached).
- [x] xUnit tests
---

### 1.5.1 — Catalog Module: Repository Architecture Refactoring `[Catalog]`
> **Goal:** Standardize all Catalog command and query handlers to use `ICatalogRepository<TEntity>` (write) and `ICatalogReadRepository<TEntity>` (read/no-tracking) instead of injecting raw `CatalogDbContext` / `CatalogReadDbContext`, and utilize `SaveInclude` for zero-read partial updates.

#### 🏛️ Phase 1.4 Handlers Refactoring (Genres & Artists)
- [x] `CreateGenreCommandHandler` → inject `ICatalogRepository<Genre>`
- [x] `UpdateGenreCommandHandler` → inject `ICatalogRepository<Genre>` + use `SaveInclude` for Name/Type updates
- [x] `ListGenresQueryHandler` → inject `ICatalogReadRepository<Genre>`
- [x] `ApplyForArtistAccountCommandHandler` → inject `ICatalogRepository<ArtistAccountApproval>`
- [x] `ApproveArtistAccountCommandHandler` → inject `ICatalogRepository<ArtistAccountApproval>`, `ICatalogRepository<Artist>` + use `SaveInclude` for Status
- [x] `RejectArtistAccountCommandHandler` → inject `ICatalogRepository<ArtistAccountApproval>` + use `SaveInclude` for Status/RejectionReason
- [x] `ListArtistAccountApprovalsQueryHandler` → inject `ICatalogReadRepository<ArtistAccountApproval>`
- [x] `GetMyArtistApplicationStatusQueryHandler` → inject `ICatalogReadRepository<ArtistAccountApproval>`
- [x] `GetArtistProfileQueryHandler` → inject `ICatalogReadRepository<Artist>`, `ICatalogReadRepository<Album>`, `ICatalogReadRepository<Track>`


---

### 1.6 — Playlist Module: Core Playlists & Library `[Playlist]`
**Features:** Full playlist lifecycle (Create/Edit/Delete), track curation (Add/Remove/Reorder), Liked Content (Tracks/Albums/Playlists), and comprehensive Library/Browse queries.  
**Tables:** `Playlist.Playlists`, `Playlist.PlaylistTracks`, `Playlist.LikedTracks`, `Playlist.LikedAlbums`, `Playlist.LikedPlaylists`

#### 📋 Playlist Management Flows (Commands)
- [x] `CreatePlaylistCommand` `[Listener]` → Creates custom playlist (Title, Description, Visibility: Private/Public/Collaborative).
- [x] `EditPlaylistCommand` `[Listener]` → Updates playlist metadata (Title, Description, Visibility).
- [x] `DeletePlaylistCommand` `[Listener]` → Soft deletes playlist (checks ownership, 403 Forbidden if `IsSystem = true`).

#### 🎵 Playlist Track Operations (Commands)
- [x] `AddTrackToPlaylistCommand` `[Listener]` → Adds track to playlist at `Position = (MaxPosition + 1)`, updates denormalized `TrackCount` and `TotalDurationSeconds`.
- [x] `RemoveTrackFromPlaylistCommand` `[Listener]` → Removes track from playlist, re-gaps remaining track positions, updates counts.
- [x] `ReorderPlaylistTracksCommand` `[Listener]` → Updates track positions (drag-and-drop support: moves track from source position to destination position and shifts intermediate tracks).

#### ❤️ Likes & Saved Content (Commands)
- [x] `LikeTrackCommand` `[Listener]` → Inserts `Playlist.LikedTracks` + automatically appends track to user's system "Liked Songs" playlist (`PlaylistTracks`) + increments `Catalog.Tracks.LikeCount`.
- [x] `UnlikeTrackCommand` `[Listener]` → Deletes from `Playlist.LikedTracks` and removes from system "Liked Songs" playlist + decrements `Catalog.Tracks.LikeCount`.
- [x] `LikeAlbumCommand` / `UnlikeAlbumCommand` `[Listener]` → Saves/unsaves album to user's library (`Playlist.LikedAlbums`).
- [x] `LikePlaylistCommand` / `UnlikePlaylistCommand` `[Listener]` → Saves/follows another user's public playlist (`Playlist.LikedPlaylists`).

#### 🎧 Playlist & Library Queries (Read Side)
- [x] `GetPlaylistQuery` → **Full Playlist View (`/playlist/:id`):** Full details (Title, Description, CoverImageUrl, Owner, TrackCount, TotalDurationSeconds, FollowerCount, IsLikedByCurrentUser) + ordered track list with artist credits and like status.
- [x] `GetLikedSongsPlaylistQuery` → **Liked Songs View (`/collection/tracks`):** Returns user's system "Liked Songs" playlist with all liked tracks ordered by `AddedAt DESC`.
- [x] `GetMyPlaylistsSimpleQuery` → **"Add to Playlist" Modal & Quick Menu:** Lightweight list of user's editable playlists (`Id`, `Title`, `CoverImageUrl`, `TrackCount`, `ContainsTrack` boolean for given `TrackId`).
- [x] `GetLibraryQuery` → **Sidebar & `/library` Page:** Aggregated view of user's library items (Owned Playlists, Liked Playlists, Liked Albums, Followed Artists) with type filtering (`all` | `playlists` | `albums` | `artists`) and sorting (`recently_added`, `alphabetical`, `creator`).
- [x] `ListPublicPlaylistsQuery` → **Search & Explore:** Paginated public playlists with search term, mood/genre, and popularity filters (Redis cached).
- [x] `GetUserPublicPlaylistsQuery` → **Profile View:** List of public playlists created by a specific user/artist profile (`/user/:id` or `/artist/:id`).

#### 📨 Messaging & Event Consumers (Cross-Module)
- [x] `UserRegisteredConsumer` `[Playlist]` → Consumes `UserRegisteredEvent` from Identity module to automatically provision the system "Liked Songs" playlist (`IsSystem = true`, `Visibility = Private`) for every new user.
- [x] xUnit tests

---

### ~~1.7 — SharedKernel Module: Outbox Processor `[SharedKernel]`~~ (CANCELED — Replaced by MassTransit Transactional Outbox)
> **Status:** *Canceled & Superseded.* Replaced by MassTransit's native EF Core Transactional Outbox and built-in background delivery service (`BusOutboxNotificationService` & `OutboxDeliveryService`). Custom polling worker is no longer needed.

---

### 1.8 — Frontend Shell `[Frontend]`
**Technologies:** React + Vite, React Router v6, TanStack Query, Axios, Tailwind CSS, React Context + useReducer

- [x] Project scaffold with Vite
- [x] Routing: `/`, `/login`, `/register`, `/artist/:id`, `/album/:id`, `/playlist/:id`, `/library`
- [x] Axios instance with JWT interceptor (attach token, refresh on 401)
- [x] Auth context (login state, user role)
- [x] Player context shell (useReducer / state structure + playback engine)
- [x] Spotify UI Layout & Pages — authentic dark theme, 3-panel shell, topbar blur, bottom player bar, responsive grids

---

### 1.9 — User Profile & Cover Images `[Identity]`
**Features:** Support profile cover/banner images and clean up profile images at the User level.
- [ ] Add `CoverImageUrl` to `Identity.UserProfiles` (EF migration)
- [ ] Update `UserProfile` entity class and configuration
- [ ] Create `UpdateUserProfileImagesCommand` (allows updating `ProfilePicUrl` and `CoverImageUrl`)
- [ ] Update profile queries and dtos to include `CoverImageUrl`
- [ ] xUnit tests

---

### 1.9.5 — Brainstorm: Notification Module `[SharedKernel]` `[Identity]` `[Social]`
> **Status:** *Deferred to Phase 3.* Initial brainstorm concluded that in-app notifications (bell, SignalR push, read/unread tracking, preferences) belong inside the `Social` module, since ~90% of notification triggers are social events (follows, posts, new releases from followed artists). Transactional emails (OTP, password reset, welcome) stay in each module using SharedKernel's `IEmailService`. If Social's notification scope outgrows the module, extract to a dedicated `SoundWave.Notification` module at that point.  
> **Reference:** [`agent/plans/notification_vision.md`](file:///c:/Users/Ahmad/Projects/SoundWave/agent/plans/notification_vision.md) (architectural vision doc, kept for Phase 3 reference).
- [x] Evaluate domain boundaries: cross-module events (`TrackReady`, `ArtistApproved`, `UserFollowedArtist`, `CollabInvite`)
- [x] Analyze table ownership → decided: `Social.Notifications` (Social module owns notifications until extraction is warranted)
- [x] Assess future real-time requirements (SignalR push notification hub & MassTransit consumers)
- [x] Decision: defer implementation to Phase 3 — Social module will own in-app notifications

---

### 1.9.6 — API Standardization `[SharedKernel]`
**Goal:** Standardize API tags, URL conventions, endpoint routes, response envelopes, and shared DbContext infrastructure across all modules.
- [x] Review and standardize OpenAPI / Swagger tags across all 46 endpoints (granular sub-tags: `"Auth"`, `"Genres"`, `"Albums"`, `"Tracks"`, `"Artists"`, `"Playlists"`, `"Playlist Tracks"`, `"Likes"`, `"Library"`)
- [x] Audit and align route URL naming conventions:
  - Prefixed Identity routes with `api/v1/auth/`
  - Moved Playlist like/unlike endpoints under `api/v1/playlists/likes/`
  - Renamed Identity `SCHEMA_NAME` to `"Auth"` and added EF migration `RenameIdentitySchemaToAuth`
  - Configured global `api/v1` route group in `Program.cs` and stripped redundant prefixes from all 46 endpoints
  - Aligned frontend client service routes (`authService.ts`, `api.ts`, `catalogService.ts`)
- [x] Standardize response envelopes: verified `SuccessResponse<PaginatedResponse<T>>` is uniformly returned across all paginated endpoints
- [x] Shared Base DbContext: extracted `BaseModuleDbContext` and `BaseModuleReadDbContext` in `SharedKernel/Data` to centralize audit stamping, schema scoping, and outbox setup across all 5 module contexts
- [x] Entity self-manipulation — evaluated; pragmatic handler validation retained, adopt entity domain methods incrementally where complex invariants emerge

---

### 1.9.7 — Catalog Module: Email Ownership & Cross-Module UserLookup `[Catalog]` `[Identity]`
**Goal:** Make Catalog own its own artist-related emails instead of routing through Identity as a middleman. Add a read-only `UserLookup` entity in `CatalogReadDbContext` mapped to `Identity.Users` for resolving user name/email directly.

- [x] Add read-only `UserLookup` entity (Id, Email, FirstName, LastName) in `CatalogReadDbContext` mapped to `Auth.Users` & `Auth.UserProfiles` tables — no FK, no write operations, `AsNoTracking` only
- [x] Create `ArtistApplicationApprovedEmailConsumer` in Catalog to resolve user data via `UserLookup` and enqueue email via `ISendEmailJob`/`IEmailService`
- [x] Create `ArtistApplicationRejectedEmailConsumer` in Catalog — same pattern
- [x] Create `ArtistApplicationSubmittedEmailConsumer` in Catalog — same pattern
- [x] Move artist-related email templates into `Catalog/Templates/EmailTemplates/`
- [x] Simplify Identity's `ArtistApplicationApprovedConsumer` to only upgrade user role to `Artist` (delete obsolete middleman consumers `ArtistApplicationSubmittedConsumer` & `ArtistApplicationRejectedConsumer`)
- [x] xUnit tests (all 244 tests passing)


---

## Phase 2 — Streaming Pipeline
> **Goal:** Upload audio, process via FFmpeg, stream HLS, record play events.  
> **New technologies:** RabbitMQ consumers, FFmpeg CLI via `System.Diagnostics.Process`, hls.js  
> **File storage:** Local disk (`IFileStorage` abstraction, `LocalFileStorage` implementation)

---

### 2.0 — Study: Audio Processing & File Storage
> **Goal:** Understand HLS streaming and file storage patterns before building the pipeline.  
> **Architecture Vision:** See detailed architecture blueprint in [file_storage_vision.md](file:///c:/Users/Ahmad/Projects/SoundWave/agent/file_storage_vision.md).  
> **Output:** You can run FFmpeg locally, produce HLS segments, and play them in a browser.

#### HLS + FFmpeg
| Topic | What to understand |
|---|---|
| What HLS is | `.m3u8` playlist + `.ts` segments — how a browser streams it |
| FFmpeg command for HLS | `ffmpeg -i input.mp3 -codec:a aac -hls_time 10 -hls_list_size 0 output.m3u8` |
| 30s preview | `-t 30` flag to cut the preview playlist |
| `System.Diagnostics.Process` | How .NET invokes FFmpeg CLI — capture exit code + stderr |
| hls.js | How the React frontend loads and plays `.m3u8` files |

**Practice exercise:** Run FFmpeg manually on an MP3 and produce HLS segments. Serve them with a simple static server and play them in the browser with hls.js. Understand the flow before building the consumer.

#### File Storage Decision
**For development:** Local file system via `IFileStorage` abstraction.

```
wwwroot/
  uploads/
    raw/{trackId}.mp3        <- uploaded file lives here
    hls/{trackId}/
      playlist.m3u8          <- FFmpeg output
      preview.m3u8
      segment_000.ts
      segment_001.ts
```

ASP.NET Core `UseStaticFiles()` serves the `wwwroot/` folder directly.  
`.gitignore` must include `wwwroot/uploads/`.

**For production (post-launch):** Swap `LocalFileStorage` for `AzureBlobStorage` or `S3FileStorage` — zero Application layer changes because of `IFileStorage` abstraction.

**Recommendation:** Start with local file system. Do not add cloud storage complexity until the core pipeline works end-to-end.

---

### 2.1 — File Storage Abstraction `[SharedKernel]`
- [ ] Define `IFileStorage` in Domain layer: `SaveAsync`, `ReadAsync`, `DeleteAsync`, `GetUrl`
- [ ] Implement `LocalFileStorage` in Infrastructure
- [ ] Serve `wwwroot/uploads/` via `UseStaticFiles()` in dev
- [ ] Unit test `LocalFileStorage`

---

### 2.1.0 — Image Upload Services & Endpoints `[SharedKernel]` `[Identity]` `[Catalog]` `[Playlist]`
**Features:** Dedicated image upload endpoints for user avatars, profile banners, album cover art, and playlist covers using `IFileStorage`.  
**Validation:** Image file size limits (max 5MB), allowed extensions (`.jpg`, `.jpeg`, `.png`, `.webp`), magic byte validation.

- [ ] `UploadProfilePictureCommand` `[Identity]` → `POST /api/v1/identity/users/me/avatar` (updates `UserProfile.ProfilePicUrl`)
- [ ] `UploadProfileBannerCommand` `[Identity]` → `POST /api/v1/identity/users/me/banner` (updates `UserProfile.CoverImageUrl`)
- [ ] `UploadAlbumCoverCommand` `[Artist]` `[Catalog]` → `POST /api/v1/catalog/albums/{albumId}/cover` (updates `Album.CoverImageUrl`)
- [ ] `UploadPlaylistCoverCommand` `[Listener]` `[Playlist]` → `POST /api/v1/playlists/{playlistId}/cover` (updates `Playlist.CoverImageUrl`)
- [ ] Image validation helper / filters (MIME type + header magic bytes check)
- [ ] xUnit tests for image uploads and validation

---

### 2.2 — Track Upload `[Catalog]`
**Features:** Artist uploads raw audio file  
**Tables:** `Catalog.Tracks` (Status=Pending), `Catalog.TrackFiles`, `SharedKernel.OutboxMessages`

- [ ] `UploadTrackCommand` `[Artist]` → validate MIME + magic bytes → stream to `IFileStorage` raw/ → insert `TrackFiles` (Status=Pending) → write `OutboxMessage` (TrackUploaded) — all in one EF transaction
- [ ] `GetTrackStatusQuery` → return `TrackFiles.Status` + `FailureReason`
- [ ] FluentValidation: max 50MB, allowed formats (mp3/flac/aac/wav) only
- [ ] xUnit tests

---

### 2.3 — FFmpeg Processing Consumer `[Catalog]`
**Features:** Consume TrackUploaded, run FFmpeg, update track status  
**Tables:** `Catalog.TrackFiles` (Status → Ready or Failed)

- [ ] RabbitMQ consumer on queue `catalog.processing`
- [ ] Read raw file path from message payload
- [ ] Invoke FFmpeg via `System.Diagnostics.Process`
  - Full: `ffmpeg -i input.mp3 -codec:a aac -hls_time 10 -hls_list_size 0 output.m3u8`
  - Preview: same with `-t 30`
- [ ] On success: `Status = Ready`, set `HlsPlaylistPath`, `PreviewPlaylistPath`, optionally clear `RawFilePath`
- [ ] On failure: `Status = Failed`, set `FailureReason` from stderr
- [ ] Emit `TrackReady` event (new `OutboxMessage`) on success — ES/Qdrant consumers subscribe to this later
- [ ] Stuck track detector `BackgroundService`: `ProcessingStartedAt > 30 min` → mark Failed
- [ ] Serilog logs FFmpeg exit code, duration, output path

---

### 2.4 — HLS Streaming Endpoints `[Streaming]`
**Features:** Serve HLS playlist, record play events  
**Tables:** `Streaming.PlayHistory`  
**Redis:** `play_count:{trackId}`, `playback_pos:{userId}:{trackId}`

- [ ] `GET /stream/{trackId}/playlist.m3u8` → 404 if Status != Ready, full playlist for auth users, preview for guests
- [ ] Play event recorded on first segment request (debounced — not per segment)
- [ ] `RecordPlayCommand` → insert `Streaming.PlayHistory` → write `OutboxMessage` (PlaybackRecorded) → Redis INCR `play_count:{trackId}`
- [ ] Play count flush `BackgroundService` → every 5 min → flush Redis counters to `Catalog.Tracks.PlayCount`
- [ ] Save/restore playback position via Redis `playback_pos:{userId}:{trackId}`
- [ ] `GetResumePositionQuery` → return Redis position or 0

---

### 2.5 — Queue & Playback Session `[Streaming]`
**Features:** Queue management, shuffle, repeat, persist queue  
**Tables:** `Streaming.QueueSessions`  
**Redis:** `queue:{userId}`

- [ ] `UpdateQueueCommand` → write to Redis (fast) → sync to `QueueSessions` DB periodically
- [ ] `GetQueueQuery` → read from Redis, fall back to DB on cache miss
- [ ] Queue session includes: current track, position, shuffle flag, repeat mode, context

---

### 2.6 — Frontend: Player `[Frontend]`
**Technologies:** hls.js, React Context + useReducer

- [ ] Wire hls.js into player context
- [ ] Load `.m3u8` on play
- [ ] Play / pause / seek / volume controls
- [ ] Progress bar with scrubbing
- [ ] Skip next / previous (> 3s into track → restart; < 3s → go back)
- [ ] Shuffle and repeat mode buttons (Off / Repeat All / Repeat One)
- [ ] Now playing bar — persistent bottom bar across all pages
- [ ] Queue drawer UI
- [ ] Poll `GET /tracks/{id}/status` after upload to enable play button when Ready

---

## Phase 3 — Social & Notifications
> **Goal:** Following system, artist posts, notification bell, home feed, collaborative playlists.  
> **New technologies:** SignalR (`NotificationHub`), RabbitMQ fan-out consumers for notifications  
> **Tables:** All `Social.*` tables, including `Social.Notifications` and `Social.NotificationPreferences`  
> **Notification Ownership:** Social module owns in-app notifications (bell icon, read/unread, SignalR push, user preferences). Transactional emails remain in each source module via SharedKernel's `IEmailService`. If notification complexity outgrows Social, extract to a dedicated module. See [`notification_vision.md`](file:///c:/Users/Ahmad/Projects/SoundWave/agent/plans/notification_vision.md) for architectural reference.

---

### 3.1 — Following System `[Social]`
**Features:** Follow/unfollow artists and users  
**Tables:** `Social.ArtistFollows`, `Social.UserFollows`

- [ ] `FollowArtistCommand` → insert `ArtistFollows` → increment `Catalog.Artists.FollowerCount` (cross-module, app enforces) → write `OutboxMessage` (UserFollowedArtist)
- [ ] `UnfollowArtistCommand` → delete + decrement
- [ ] `FollowUserCommand` / `UnfollowUserCommand` → update both follower/following counts on `Identity.Users`
- [ ] `GetFollowersQuery` / `GetFollowingQuery` → paginated lists
- [ ] xUnit tests

---

### 3.2 — Artist Posts `[Social]`
**Features:** Artist publishes update post, followers see it  
**Tables:** `Social.ArtistPosts`

- [ ] `CreateArtistPostCommand` `[Artist]` → insert `Social.ArtistPosts` → write `OutboxMessage` (ArtistPublishedPost)
- [ ] Consumer: batch insert `Social.Notifications` for all followers
- [ ] `GetArtistPostsQuery` → paginated by artist
- [ ] `DeleteArtistPostCommand` → soft delete
- [ ] xUnit tests

---

### 3.3 — Home Feed `[Social]` `[Frontend]`
**Features:** Aggregated feed — new releases, artist posts, recently played

- [ ] `GetHomeFeedQuery` → join `ArtistFollows` + `ArtistPosts` + new `Catalog.Albums` from followed artists + last 5 from `Streaming.PlayHistory`
- [ ] Frontend home page rendering feed cards

---

### 3.4 — Collaborative Playlists `[Playlist]`
**Features:** Owner invites collaborators, collaborators can edit  
**Tables:** `Playlist.PlaylistCollaborators`

- [ ] `InviteCollaboratorCommand` → insert `PlaylistCollaborators` → write `OutboxMessage` (CollabInvite)
- [ ] `RemoveCollaboratorCommand`
- [ ] `AddTrackToPlaylistCommand` now checks ownership OR collaborator permission
- [ ] xUnit tests

---

## Phase 4 — Analytics
> **Goal:** Artist dashboard, user personal stats, admin platform dashboard.  
> **New technologies:** .NET `BackgroundService` nightly Worker  
> **Tables:** All `Analytics.*` tables

---

### 4.1 — Analytics Rollup Worker `[Analytics]`
**Features:** Nightly aggregation from PlayHistory into all stats tables

- [ ] `AnalyticsRollupWorker` — `BackgroundService` runs nightly
- [ ] Reads `Streaming.PlayHistory` for yesterday
- [ ] Upserts `Analytics.DailyTrackStats` per track
- [ ] Upserts `Analytics.DailyArtistStats` per artist
- [ ] Upserts `Analytics.MonthlyUserStats` per user
- [ ] Upserts `Analytics.PlatformDailyStats` for yesterday
- [ ] Serilog logs duration + rows processed

---

### 4.2 — Artist Dashboard `[Analytics]` `[Frontend]`
**Features:** Stream chart, top tracks, unique listeners, follower trend

- [ ] `GetArtistDashboardQuery` → reads `DailyTrackStats` + `DailyArtistStats`
- [ ] `GetArtistTopTracksQuery` → `DailyTrackStats` grouped by `TrackId`, date range param
- [ ] `GetFollowerTrendQuery` → `FollowerGained - FollowerLost` per day from `DailyArtistStats`
- [ ] Frontend: line charts, top tracks list

---

### 4.3 — User Listening Stats `[Analytics]` `[Frontend]`
**Features:** Top tracks, top artists, total listen time, Wrapped equivalent

- [ ] `GetUserMonthlyStatsQuery` → reads `MonthlyUserStats` for current month
- [ ] `GetUserWrappedQuery` → aggregate all 12 `MonthlyUserStats` rows for a given year
- [ ] Frontend: stats page, yearly Wrapped reveal

---

### 4.4 — Admin Dashboard `[Analytics]` `[SharedKernel]` `[Frontend]`
**Features:** DAU chart, top charts, upload health, audit log viewer, user/artist management

- [ ] `GetPlatformStatsQuery` `[Admin]` → reads `PlatformDailyStats`
- [ ] `GetTopChartsQuery` → `Catalog.Tracks` ordered by `PlayCount DESC`
- [ ] `GetAuditLogsQuery` `[Admin]` → paginated `SharedKernel.AuditLogs`
- [ ] `GetAllUsersQuery` / `GetAllArtistsQuery` `[Admin]` → paginated with filters
- [ ] `LockUserCommand` / `UnlockUserCommand` `[Admin]` → update + bulk revoke refresh tokens + write `AuditLogs`
- [ ] Frontend: admin panel pages

---

## Phase 5 — Search & Discovery
> **Goal:** Full-text search via ElasticSearch, semantic search and recommendations via Qdrant.  
> **New technologies:** ElasticSearch 8 (`Elastic.Clients.Elasticsearch`), Qdrant (.NET SDK)  
> **Add to Docker Compose:** ElasticSearch + Qdrant containers

---

### 5.1 — ElasticSearch Setup & Indexing `[Catalog]`
**Features:** Index tracks when TrackReady fires, index artists when ArtistApproved fires

- [ ] RabbitMQ consumer on `catalog.search` queue (subscribes to `TrackReady` routing key)
- [ ] Index document: trackId, title, artistNames, albumTitle, genres, moods, duration, isExplicit, releaseYear
- [ ] Consumer on `catalog.artist.index` (subscribes to `ArtistApproved`)
- [ ] Re-index on track metadata edit
- [ ] Remove from index on soft delete

---

### 5.2 — Search Endpoints `[Catalog]` `[Frontend]`
**Features:** Full-text search, filters, autocomplete, recent searches  
**Tables:** `Streaming.SearchHistory`  
**Redis:** `autocomplete:{prefix}` TTL 5 min

- [ ] `SearchQuery` → ES query with filters (genre, mood, year range, explicit, duration)
- [ ] `AutocompleteQuery` → ES prefix query, Redis cached per prefix
- [ ] `SaveSearchHistoryCommand` → insert `SearchHistory` (delete oldest if > 50 rows)
- [ ] `GetRecentSearchesQuery` → last 10 from `SearchHistory`
- [ ] Frontend: search bar with autocomplete dropdown, results page with filter panel

---

### 5.3 — Qdrant Setup & Embeddings `[Catalog]`
**Features:** Store track vectors for semantic search and recommendations

- [ ] RabbitMQ consumer on `catalog.embeddings` queue (subscribes to `TrackReady`)
- [ ] Generate embedding vector from genre/mood tags + metadata
- [ ] Upsert into Qdrant collection `tracks` with payload `{ trackId, artistId, genreIds }`
- [ ] Feed `Streaming.PlayHistory` signals (liked = positive, skipped = negative) into user vectors periodically via Worker

---

### 5.4 — Recommendation Endpoints `[Catalog]` `[Frontend]`
**Features:** Sounds like this, Recommended for you, Because you liked X

- [ ] `GetSimilarTracksQuery` → Qdrant nearest neighbours on track vector
- [ ] `GetRecommendedForUserQuery` → Qdrant nearest neighbours on user behaviour vector
- [ ] `GetBecauseYouLikedQuery` → given a liked track, find similar via Qdrant
- [ ] Frontend: recommendation rows on home page and track pages

---

## Phase 6 — Polish & Production Readiness
> **Goal:** Everything works end-to-end, tested, secure, ready to deploy.

### 6.1 — Testing Pass
- [ ] Integration tests with Testcontainers (real MSSQL container)
- [ ] 80%+ coverage on all Application layer handlers
- [ ] FluentValidation tests for every command validator
- [ ] RabbitMQ consumer tests (mock channel, test message handling logic)

### 6.2 — Security Hardening
- [ ] MIME type + magic byte validation on file uploads
- [ ] Stricter rate limiting on auth endpoints
- [ ] HTTPS enforced
- [ ] CORS locked to frontend origin only
- [ ] No stack traces in production error responses

### 6.3 — Observability
- [ ] Correlation IDs propagated to all RabbitMQ message headers
- [ ] Serilog enrichers: `UserId`, `TrackId`, `CorrelationId` on every relevant log
- [ ] Health checks cover: MSSQL, Redis, RabbitMQ, ES, Qdrant
- [ ] Slow query logging in EF Core (log queries > 500ms)

### 6.4 — Frontend Polish
- [ ] Responsive layout
- [ ] Loading skeletons on all async data
- [ ] Error boundaries
- [ ] Empty states
- [ ] Toast notifications for async actions (upload submitted, track processing complete, etc.)

### 6.5 — File Storage Swap (Optional)
- [ ] Implement `AzureBlobStorage : IFileStorage` or `S3FileStorage : IFileStorage`
- [ ] Register in DI instead of `LocalFileStorage` — zero other changes needed
- [ ] Pre-signed URL generation for HLS serving via CDN

---

## Technology Decision Summary

| Concern | Technology | Notes |
|---|---|---|
| Backend framework | .NET 8 Web API | |
| Frontend | React + Vite | |
| Primary database | MSSQL | 7 schemas: Identity, Catalog, Streaming, Playlist, Social, Analytics, SharedKernel |
| ORM | EF Core 8 | One DbContext per module, Fluent API only, no data annotations |
| ID strategy | `Guid.CreateVersion7()` | Sequential GUIDs — no fragmentation, generated in app before DB insert |
| Cross-schema FKs | None | Plain `Guid` properties, application enforces consistency |
| CQRS | MediatR | Commands mutate state, queries use `AsNoTracking()` and return DTOs |
| Validation | FluentValidation | Pipeline behaviour — runs before every command handler automatically |
| Error handling | Result\<T\> pattern | No business exceptions — errors returned as values |
| Auth | Custom JWT | 15 min access token + 7 day refresh token, BCrypt passwords |
| Caching / sessions | Redis (StackExchange.Redis) | Play counts, queue, positions, token blacklist, rate limiting |
| Event bus | RabbitMQ raw client | No MassTransit — `RabbitMQ.Client` directly, `IEventBus` abstraction in Domain |
| Outbox | `SharedKernel.OutboxMessages` table | Written in same EF transaction as business data, Worker publishes |
| Audio processing | FFmpeg CLI | `System.Diagnostics.Process`, outputs HLS segments |
| Audio streaming | HLS (.m3u8 + .ts) | hls.js on frontend, static files middleware in dev |
| File storage (dev) | Local disk | `wwwroot/uploads/` — `IFileStorage` abstraction, swap to Azure/S3 for prod |
| File storage (prod) | Azure Blob or AWS S3 | Just swap the `IFileStorage` implementation |
| Full-text search | ElasticSearch 8 | Phase 5 — never use MSSQL LIKE queries for search |
| Semantic search | Qdrant | Phase 5 — track embeddings + user behaviour vectors for recommendations |
| Logging | Serilog + Seq | Structured JSON, correlation IDs, Seq UI for dev debugging |
| Testing | xUnit + Moq | Unit tests per handler, Testcontainers for integration tests |
| Background jobs | .NET BackgroundService | OutboxProcessor, play count flush, analytics rollup, stuck track detector |
| Soft delete | EF Core global filter | `HasQueryFilter(e => !e.IsDeleted)` on all entities |
| Admin audit trail | `SharedKernel.AuditLogs` table | Admin actions only — completely separate from Serilog runtime logs |
| Architecture | Modular Monolith + Vertical Slicing | One project per module, one folder per feature/use case |

---

## Quick Reference — What Goes Where

```
You want to...                              You touch...
────────────────────────────────────────────────────────────────
Add a new feature                       →  New folder in the module: Command + Handler + Validator + Tests
Add a new DB table                      →  New entity + IEntityTypeConfiguration<T> + EF migration
Publish a RabbitMQ event                →  Write OutboxMessage in same EF transaction as business data
Consume a RabbitMQ event                →  New consumer class in Infrastructure of the relevant module
Add a background job                    →  New BackgroundService in Infrastructure
Do a Redis operation                    →  StackExchange.Redis directly in Infrastructure layer
Add an admin-only endpoint              →  [Authorize(Roles = "Admin")] + write to SharedKernel.AuditLogs
Log something                           →  ILogger<T> with named properties — never string interpolation
Search something                        →  ElasticSearch query via ISearchService abstraction
Recommend something                     →  Qdrant query via IRecommendationService abstraction
Read data for a UI screen               →  Query handler with AsNoTracking() returning a flat DTO
Mutate data                             →  Command handler with full EF tracking + SaveChangesAsync()
```

---

## Archived Phases

### 1.2 — Identity Module: Registration & Login `[Identity]`
**Features:** Register, Email verification, Login, Logout  
**Tables:** `Identity.Users`, `Identity.RefreshTokens`  
**Redis:** `email_verify:{userId}`, `login_fails:{email}`, `blacklist:{jti}`

- [x] `RegisterCommand` → BCrypt hash → insert `Identity.Users` → write `OutboxMessage` (UserRegistered)
- [x] `LoginCommand` → BCrypt verify → issue JWT (15 min) + refresh token (7 days, hashed in `RefreshTokens`)
- [x] `RefreshTokenCommand` → validate hash → rotate token → issue new JWT
- [x] `LogoutCommand` → revoke refresh token in DB → add jti to Redis blacklist
- [x] `VerifyEmailCommand` → validate Redis key → set `IsEmailVerified = 1`
- [x] Account lockout: Redis `login_fails:{email}` INCR, lock after 5
- [x] `[Authorize]` middleware wired up with role claims
- [x] FluentValidation on all commands
- [x] xUnit tests for all handlers

### 1.2.5 — Refactoring: Cache Keys `[SharedKernel]`
**Features:** Refactor caching key constants to be inline functions to handle building the key safely.

- [x] Convert `Constants.Caching` and `SharedConstants.Caching` constants into helper methods/functions that accept parameters (e.g. `UserId`, `Jti`) to prevent manual string concatenation when building cache keys.
- [x] Update all calling locations in Identity handlers and TokenService.

### 1.2.6 — Roadmap Feature: Account Lockout Refinement `[Identity]`
**Features:** Advanced lockouts (planned for later).

- [x] Implement temporary lockout on failed logins to prevent additional attempts.
- [x] If failed attempts exceed a certain threshold, apply a soft lock (user cannot login for X amount of time).
- [x] If attempts hit a higher threshold, apply a hard lock (requires admin unlock).

### 1.3 — Identity Module: Password Reset `[Identity]`
**Features:** Forgot password, Reset password  
**Redis:** `pwd_reset:{userId}` TTL 1h

- [x] `ForgotPasswordCommand` → generate token → store in Redis → log email for now
- [x] `ResetPasswordCommand` → validate Redis token → BCrypt new hash → delete Redis key
- [x] xUnit tests

