# SoundWave — Project Structure (Initial Scan)

> Scanned: 2026-04-04

## Root
```
SoundWave/
├── .dockerignore
├── .git/
├── .gitattributes
├── .github/workflows/           ← Empty (no CI/CD yet)
├── .gitignore
├── README.md                    ← Placeholder ("# SoundWave")
├── SoundWave.slnx               ← .NET solution file (XML format)
├── agent/                       ← Project docs, specs, cache
├── src/                         ← Source code
└── tests/                       ← Test projects
```

## Solution Projects (SoundWave.slnx)
```
/src/
  SoundWave.API                  ← ASP.NET Core Web API host
  SoundWave.SharedKernel         ← Shared base types / utilities
/src/Modules/
  SoundWave.Analytics            ← Analytics module (DailyTrackStats, DailyArtistStats, MonthlyUserStats, PlatformDailyStats)
  SoundWave.Catalog              ← Catalog module (Artists, Albums, Tracks, Genres)
  SoundWave.Identity             ← Identity module (Users, Auth, Tokens)
  SoundWave.Infra                ← Infra module (OutboxMessages, AuditLogs, Background Workers)
  SoundWave.Playlist             ← Playlist module (Playlists, LikedTracks, LikedAlbums, Collaborators)
  SoundWave.Social               ← Social module (UserFollows, ArtistFollows, ArtistPosts, Notifications)
  SoundWave.Streaming            ← Streaming module (PlayHistory, Queue, SearchHistory)
/tests/
  SoundWave.Analytics.Tests      ← Analytics unit tests
  SoundWave.Catalog.Tests        ← Catalog unit tests
  SoundWave.Identity.Tests       ← Identity unit tests
  SoundWave.Infra.Tests          ← Infra unit tests
  SoundWave.Playlist.Tests       ← Playlist unit tests
  SoundWave.Social.Tests         ← Social unit tests
  SoundWave.Streaming.Tests      ← Streaming unit tests
```

## Module Status — All Scaffolded, Build Passing
All 7 modules: `Class1.cs` (placeholder) + `.csproj` referencing SharedKernel
- ✅ **SoundWave.Identity** — SharedKernel ref ✓
- ✅ **SoundWave.Catalog** — SharedKernel ref ✓
- ✅ **SoundWave.Streaming** — SharedKernel ref ✓
- ✅ **SoundWave.Playlist** — SharedKernel ref ✓ (NEW)
- ✅ **SoundWave.Social** — SharedKernel ref ✓ (NEW)
- ✅ **SoundWave.Analytics** — SharedKernel ref ✓ (NEW)
- ✅ **SoundWave.Infra** — SharedKernel ref ✓ (NEW)

## Wiring Rules
- Every module → SharedKernel (for Result<T>, BaseEntity, etc. when added)
- API → all 7 modules (API is the single composition root)
- Modules do NOT reference each other — cross-module = plain Guid IDs only

## API Project
```
SoundWave.API/
├── Program.cs                   ← Entry point
├── Properties/                  ← launch settings
├── SoundWave.API.csproj
├── SoundWave.API.http           ← HTTP request file
├── appsettings.json
└── appsettings.Development.json
```

## SharedKernel Project
```
SoundWave.SharedKernel/
├── Class1.cs                    ← Placeholder
└── SoundWave.SharedKernel.csproj
```

## Tests
```
tests/SoundWave.Identity.Tests/
├── UnitTest1.cs                 ← Placeholder
└── SoundWave.Identity.Tests.csproj
```

## Agent Directory
```
agent/
├── ROADMAP.md                   ← Full project roadmap (Phase 0-6)
├── soundwave-roadmap.md         ← Duplicate of ROADMAP.md
├── soundwave-roadmap.pdf        ← PDF export of roadmap
├── soundwave-schema-v5.jsx      ← Interactive DB schema viewer (React)
├── soundwave-brd-v3.jsx         ← Business Requirements Document (React)
├── db-schema.md                 ← Simpler schema reference
├── examplestrucutre.md          ← Example module folder structure reference
├── SoundWaveSchemaApp/          ← Vite+React app for schema viewer
└── cache/                       ← AI knowledge cache (created now)
```

## Missing vs Roadmap
Items from Phase 1.1 (Project Skeleton) not yet implemented:
- [ ] DbContext per module (no EF Core setup yet)
- [ ] Global exception middleware (ProblemDetails)
- [ ] Serilog + Seq config
- [ ] Correlation ID middleware
- [ ] Health check endpoints
- [ ] Docker Compose file (MSSQL, Redis, RabbitMQ, Seq)
- [ ] BaseEntity with audit fields
- [ ] Global soft-delete query filter
- [ ] Result<T> and Error types
- [ ] GuidV7 helper
- [ ] Modules not yet structured: Playlist, Social, Analytics, Infra

## Key Observations
1. **Very early stage** — all modules are empty scaffolds with Class1.cs placeholders
2. **Solution uses .slnx format** (newer XML-based solution format)
3. **3 of 7 modules exist** — Identity, Catalog, Streaming. Missing: Playlist, Social, Analytics, Infra
4. **No Docker Compose** yet
5. **No frontend project** in src/ (only agent/SoundWaveSchemaApp which is a doc viewer)
6. **SharedKernel** exists but is empty — will hold Result<T>, BaseEntity, etc.
7. **Only 1 test project** — Identity tests, placeholder only
