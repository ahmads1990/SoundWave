# 🎵 SoundWave - Spotify Clone Backend Web API

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-14-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)
![MassTransit](https://img.shields.io/badge/MassTransit-Transactional%20Outbox-0052CC?style=for-the-badge)
![Tests](https://img.shields.io/badge/Tests-249%20Passing-brightgreen?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**A production-ready, modular monolith music streaming backend built on CQRS, Vertical Slice Architecture, and MassTransit Event-Driven Architecture**

[Features](#-features) • [Architecture](#-architecture) • [Getting Started](#-getting-started) • [API Documentation](#-api-documentation) • [Roadmap](#-project-status--roadmap) • [Contributing](#-contributing)

</div>

---

## 📖 Overview

**SoundWave** is an enterprise-grade music streaming backend API replicating core Spotify workflows. Built on **.NET 10 (C# 14)** using a **Modular Monolith** pattern, it demonstrates robust software architecture principles: physical domain boundary isolation, CQRS with read/write repository segregation, vertical slice feature packaging, and asynchronous event-driven communication via **RabbitMQ** with **MassTransit Transactional Outbox**.

🎨 **Frontend Project**: The companion React client built with React 19, Vite, and Tailwind CSS is available in the [`client/`](client/) directory of this repository.

### ✨ Key Highlights

- 🏛️ **Modular Monolith Architecture** — 5 independent business modules (`Identity`, `Catalog`, `Playlist`, `Streaming`, `Social`) with strict boundary separation and separate database schemas (`Auth`, `Catalog`, `Playlist`, `Streaming`, `Social`).
- ⚡ **CQRS & Vertical Slices** — Every feature is self-contained with its own Command/Query, Handler, Request DTO, and Validator. Handlers inject dedicated write repositories (`ICatalogRepository<T>`) or zero-tracking read repositories (`ICatalogReadRepository<T>`).
- 📬 **Event-Driven Messaging & Outbox** — Seamless inter-module messaging with MassTransit and RabbitMQ. Guaranteed at-least-once delivery via the **EF Core Transactional Outbox** (no dual-write bugs).
- 🛡️ **Zero Direct Cross-Module Queries** — Cross-module data resolution uses dedicated, unconstrained read-only lookup entities (`UserLookup`, `UserProfileLookup`) in read DB contexts.
- 🔐 **Hardened Auth & Security** — Dual-token authentication (short-lived JWT + 7-day rotating refresh tokens), JTI token revocation blacklist in Redis, rate limiting, and dual-tier soft/hard brute-force account lockout.
- 🚀 **High Performance** — Read/write DbContext segregation, Redis caching for hot endpoints (genres, top releases, public playlists), fast partial updates with `SaveInclude`, and response compression.
- 📊 **Observability** — Structured logging with Serilog and Seq correlation IDs.

---

## 🎯 Features

### 🔐 Identity & Authentication (`Auth` Schema)
- **Registration & Verification** — Listener registration with BCrypt password hashing and OTP-based email verification via Hangfire background jobs.
- **Dual-Token System** — Access tokens (15-min) with rotating refresh tokens (7 days, cryptographically hashed in database).
- **Session Revocation** — Token blacklisting in Redis upon logout (immediate JWT invalidation via middleware).
- **Account Lockout Protection** — Incremental failed login tracking in Redis; triggers temporary soft lockout (60 mins) and administrative hard lockouts after repeated failures.
- **Password Recovery** — Secure OTP-based forgot password and reset flows with expiration safeguards.
- **User Profile Management** — Dedicated profile management with avatar (`ProfilePicUrl`) and cover banner (`CoverImageUrl`) customization.

### 💿 Music Catalog & Artist Studio (`Catalog` Schema)
- **Artist Application Lifecycle** — Listeners apply for artist accounts; administrators inspect applications in a paginated dashboard with one-click approval/rejection and automated transactional email dispatch.
- **Release Management (Studio)**:
  - **1-Step Single Release**: Atomically creates Album container and Track in a single request.
  - **Multi-Track Builder**: Create empty Albums/EPs, add tracks incrementally, update metadata, and publish when ready.
  - **Track Management**: Full re-ordering, re-gapping, collaborative featured artist tagging, and multi-genre tagging.
- **Public Catalog Discovery**:
  - Full album views with ordered tracklists, artist credits, and genre tags.
  - Home screen carousel with recently released albums (Redis cached).
  - Search and filter releases by genre, artist, and publication status.

### 📋 Playlists & User Library (`Playlist` Schema)
- **Playlist Lifecycle** — Create, edit, and soft-delete custom playlists with Public, Private, or Collaborative visibility modes.
- **Track Curation** — Add, remove, and reorder playlist tracks with drag-and-drop position shifts.
- **Automatic Liked Songs Provisioning** — Consumes `UserRegisteredEvent` from Identity to automatically provision the system "Liked Songs" playlist for each user.
- **Library Likes & Saved Content** — Like tracks (appends to system playlist & syncs count), save albums to library, and follow public playlists.
- **Unified Library Explorer** — Aggregated library querying with type filtering (`all`, `playlists`, `albums`, `artists`) and sorting.

### 🎧 Streaming Pipeline (`Streaming` Schema - Phase 2)
- **Media Ingestion & Validation** — Strict magic-byte inspection (detects genuine audio signatures vs disguised files).
- **HLS Transcoding Worker** — Asynchronous background worker running FFmpeg in Docker to slice audio into 6-second `.ts` segments and `.m3u8` playlists.
- **Free Previews** — Auto-generated 30-second preview streams for unauthenticated listeners.
- **S3 Storage Abstraction** — Pluggable `IFileStorage` contract supporting Local Disk, RustFS, and AWS S3/Cloudflare R2.
- **Play Count Analytics** — Redis-debounced play event registration to prevent play-count fraud.

---

## 🏗️ Architecture

SoundWave is built as a **Modular Monolith**, striking the ideal balance between microservice-level domain isolation and monolithic operational simplicity.

```
                               ┌─────────────────────────┐
                               │   SoundWave Web Client  │
                               │   (React 19 + Vite UI)  │
                               └────────────┬────────────┘
                                            │ HTTP / JSON
                                            ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                    SoundWave.API                                       │
│                Minimal APIs • Global Group (/api) • OpenAPI / ScalarDocs               │
└───────────────────┬───────────────────┬───────────────────┬───────────────────┬────────┘
                    │                   │                   │                   │
        ┌───────────▼────────┐  ┌───────▼──────────┐  ┌─────▼──────────┐  ┌─────▼──────────┐
        │  Identity Module   │  │  Catalog Module  │  │Playlist Module │  │Streaming Module│
        │   [Auth Schema]    │  │ [Catalog Schema] │  │[Playlist Schema│  │[Streaming Sch.]│
        ├────────────────────┤  ├──────────────────┤  ├────────────────┤  ├────────────────┤
        │ • Vertical Slices  │  │ • Vertical Slices│  │• Vertical Slices│ │ • Transcoding  │
        │ • MediatR Handlers │  │ • Write/Read Repo│  │• Write/Read Repo│ │ • HLS Playlists│
        │ • BaseModuleDbCtx  │  │ • BaseModuleDbCtx│  │• BaseModuleDbCtx│ │ • Stream Serve │
        └───────────┬────────┘  └───────┬──────────┘  └─────┬──────────┘  └─────┬──────────┘
                    │                   │                   │                   │
                    │         MassTransit Transactional Outbox (EF Core)        │
                    └───────────────────┬───────────────────┴───────────────────┘
                                        │ Events: UserRegistered, ArtistApproved...
                                        ▼
                           ┌─────────────────────────┐
                           │   RabbitMQ Message Bus  │
                           └────────────┬────────────┘
                                        │ Async Consumer Workers
                                        ▼
                           ┌─────────────────────────┐
                           │   Background Consumers  │
                           │ Transcode • Emails • Out│
                           └─────────────────────────┘
```

### Module Boundary Rules
1. **Physical Isolation:** Each module is a standalone `.csproj` assembly under `src/Modules/`.
2. **Schema Separation:** Each module manages its own SQL Server schema (`Auth`, `Catalog`, `Playlist`, `Streaming`, `Social`).
3. **No Direct References:** A module never references another module's DbContext or internal entities.
4. **Cross-Module Communication:** Handled via:
   - **Asynchronous Events**: MassTransit events (`UserRegisteredEvent`, `ArtistApprovedEvent`) published to RabbitMQ.
   - **Cross-Module Read Lookups**: Read-only, unconstrained lookup entities in read contexts (`UserLookup` in `CatalogReadDbContext` mapped to `Auth.Users` without foreign key constraints).

---

## 🔌 API Documentation

All endpoints are organized under the global `/api` route group and versioned with `/v1/`.

### 🔐 Auth Endpoints (`/api/v1/auth/`)
| Method | Route | Auth | Description |
|--------|-------|:----:|-------------|
| `POST` | `/api/v1/auth/register` | 🌐 | Register listener account (writes outbox event) |
| `POST` | `/api/v1/auth/login` | 🌐 | Authenticate & issue JWT + refresh token |
| `POST` | `/api/v1/auth/logout` | 🔒 | Revoke refresh token & blacklist JTI in Redis |
| `POST` | `/api/v1/auth/refresh-tokens` | 🌐 | Rotate refresh token & issue new JWT |
| `POST` | `/api/v1/auth/verify-email` | 🌐 | Confirm email address with 6-digit OTP |
| `POST` | `/api/v1/auth/verify-email/resend` | 🌐 | Resend verification email OTP |
| `POST` | `/api/v1/auth/password/forgot` | 🌐 | Request password reset OTP |
| `POST` | `/api/v1/auth/password/reset` | 🌐 | Reset password using OTP |
| `GET`  | `/api/v1/auth/profile/me` | 🔒 | Get current user's profile details & images |
| `PUT`  | `/api/v1/auth/profile/images` | 🔒 | Update profile avatar and banner cover URLs |

### 🎼 Catalog & Studio Endpoints (`/api/v1/catalog/`)
| Method | Route | Auth | Description |
|--------|-------|:----:|-------------|
| `GET`  | `/api/v1/catalog/genres` | 🌐 | List all music genres/moods (Redis cached) |
| `POST` | `/api/v1/catalog/genres` | 👑 | Create new music genre/mood |
| `PUT`  | `/api/v1/catalog/genres/{id}` | 👑 | Update genre/mood name and type |
| `GET`  | `/api/v1/catalog/artists/{id}` | 🌐 | Get artist profile, top tracks, and albums |
| `POST` | `/api/v1/catalog/artists/apply` | 🔒 | Submit application for artist verification |
| `GET`  | `/api/v1/catalog/artists/applications/me`| 🔒 | Check caller's artist application status |
| `GET`  | `/api/v1/catalog/artists/applications` | 👑 | List pending/reviewed applications |
| `POST` | `/api/v1/catalog/artists/applications/{id}/approve` | 👑 | Approve application & promote user to Artist |
| `POST` | `/api/v1/catalog/artists/applications/{id}/reject` | 👑 | Reject application with reason |
| `GET`  | `/api/v1/catalog/albums` | 🌐 | Browse & filter albums (paginated, cached) |
| `GET`  | `/api/v1/catalog/albums/{id}` | 🌐 | Get album details with tracklist & credits |
| `GET`  | `/api/v1/catalog/albums/new-releases` | 🌐 | Home carousel of recent releases (cached) |
| `POST` | `/api/v1/catalog/albums` | 🎤 | Create Album/EP container release builder |
| `POST` | `/api/v1/catalog/albums/single` | 🎤 | 1-step atomic Single release builder |
| `PUT`  | `/api/v1/catalog/albums/{id}` | 🎤 | Update album metadata (title, cover, genres) |
| `POST` | `/api/v1/catalog/albums/{id}/publish` | 🎤 | Validate tracklist & publish to listeners |
| `POST` | `/api/v1/catalog/tracks` | 🎤 | Add new track to album release |
| `PUT`  | `/api/v1/catalog/tracks/{id}` | 🎤 | Edit track metadata (title, genres, collabs) |
| `DELETE`| `/api/v1/catalog/tracks/{id}` | 🎤 | Soft delete track & re-gap track numbers |
| `POST` | `/api/v1/catalog/tracks/{id}/move` | 🎤 | Move track to another album release |

### 📋 Playlist & Library Endpoints (`/api/v1/playlists/`, `/api/v1/library`)
| Method | Route | Auth | Description |
|--------|-------|:----:|-------------|
| `GET`  | `/api/v1/playlists` | 🌐 | Browse public playlists (cached, paginated) |
| `GET`  | `/api/v1/playlists/{id}` | 🌐 | Get playlist details with tracklist & likes |
| `GET`  | `/api/v1/playlists/me` | 🔒 | Simple list for "Add to Playlist" modal |
| `GET`  | `/api/v1/playlists/collection/tracks` | 🔒 | Get user's system "Liked Songs" playlist |
| `POST` | `/api/v1/playlists` | 🔒 | Create custom playlist |
| `PUT`  | `/api/v1/playlists/{id}` | 🔒 | Edit playlist metadata (title, visibility) |
| `DELETE`| `/api/v1/playlists/{id}` | 🔒 | Soft delete custom playlist |
| `POST` | `/api/v1/playlists/{id}/tracks` | 🔒 | Append track to playlist |
| `DELETE`| `/api/v1/playlists/{id}/tracks/{trackId}` | 🔒 | Remove track from playlist & re-gap |
| `PUT`  | `/api/v1/playlists/{id}/tracks/reorder` | 🔒 | Reorder track positions (drag & drop) |
| `POST` | `/api/v1/playlists/likes/tracks/{trackId}` | 🔒 | Like track (syncs system playlist & count) |
| `DELETE`| `/api/v1/playlists/likes/tracks/{trackId}` | 🔒 | Unlike track |
| `POST` | `/api/v1/playlists/likes/albums/{albumId}` | 🔒 | Save album to library |
| `DELETE`| `/api/v1/playlists/likes/albums/{albumId}` | 🔒 | Unsave album from library |
| `POST` | `/api/v1/playlists/likes/{playlistId}` | 🔒 | Save/follow public playlist |
| `DELETE`| `/api/v1/playlists/likes/{playlistId}` | 🔒 | Unfollow public playlist |
| `GET`  | `/api/v1/library` | 🔒 | Aggregated user library view |
| `GET`  | `/api/v1/users/{id}/playlists` | 🌐 | Public playlists created by user/artist |

*Legend: 🌐 = Public Endpoint | 🔒 = Authenticated Listener | 🎤 = Artist Role | 👑 = Admin Role*

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server, Redis, and RabbitMQ)
- Node.js 20+ (for client)

### 1. Clone the Repository
```bash
git clone https://github.com/ahmads1990/SoundWave.git
cd SoundWave
```

### 2. Start Infrastructure via Docker Compose
```bash
docker-compose up -d
```
This provisions:
- **SQL Server 2022** on `localhost:1433`
- **Redis** on `localhost:6379`
- **RabbitMQ (with Management Console)** on `localhost:5672` (Console: `localhost:15672`)
- **Seq (Structured Logging Dashboard)** on `localhost:5341`

### 3. Apply Database Migrations
```bash
dotnet ef database update --project src/SoundWave.API/SoundWave.API.csproj
```

### 4. Run the API
```bash
dotnet run --project src/SoundWave.API/SoundWave.API.csproj
```
Navigate to:
- **Interactive OpenAPI / Scalar Docs**: `https://localhost:PORT/scalar/v1`
- **Hangfire Dashboard**: `https://localhost:PORT/hangfire`
- **Seq Log Viewer**: `http://localhost:5341`

---

## 🧪 Testing

The solution includes comprehensive unit and integration tests powered by **xUnit**, **FluentAssertions**, **Moq**, and **Testcontainers** (spinning up isolated real SQL Server test instances).

```bash
dotnet test
```

```
Passed!  - Failed: 0, Passed:  51, Skipped: 0 - SoundWave.Identity.Tests.dll
Passed!  - Failed: 0, Passed: 124, Skipped: 0 - SoundWave.Catalog.Tests.dll
Passed!  - Failed: 0, Passed:  71, Skipped: 0 - SoundWave.Playlist.Tests.dll
Passed!  - Failed: 0, Passed:   1, Skipped: 0 - SoundWave.Social.Tests.dll
Passed!  - Failed: 0, Passed:   1, Skipped: 0 - SoundWave.Streaming.Tests.dll
Passed!  - Failed: 0, Passed:   1, Skipped: 0 - SoundWave.Analytics.Tests.dll
-------------------------------------------------------------------------------
Total: 249 Passed, 0 Failed, 0 Skipped
```

---

## 🗺️ Project Status & Roadmap

| Phase | Description | Module(s) | Status |
|:-----:|-------------|:---------:|:------:|
| **1.0** | Core Architecture & Study | Cross-Cutting | ✅ Completed |
| **1.1** | Project Skeleton & Base Monolith | `SharedKernel` | ✅ Completed |
| **1.2** | Identity: Registration, Login, Token Blacklist | `Identity` | ✅ Completed |
| **1.3** | Identity: Password Reset & Email Verification | `Identity` | ✅ Completed |
| **1.4** | Catalog: Genres, Artists & Artist Applications | `Catalog` | ✅ Completed |
| **1.5** | Catalog: Album/Single Release Builders & Tracks | `Catalog` | ✅ Completed |
| **1.6** | Playlists: Curation, Reordering, Likes & Library | `Playlist` | ✅ Completed |
| **1.8** | Frontend Shell: Spotify Dark Theme & Player Context | `Frontend` | ✅ Completed |
| **1.9** | User Profile: Avatars & Banner Covers | `Identity` | ✅ Completed |
| **1.9.6** | API Standardization & Base DbContext Extraction | `SharedKernel` | ✅ Completed |
| **1.9.7** | Decoupled Emails & Cross-Module UserLookup | `Catalog` | ✅ Completed |
| **2.0** | Study: Audio Processing, HLS & Storage Patterns | `Streaming` | 🔄 In Progress |
| **2.1** | File Storage Abstraction (`IFileStorage`) | `SharedKernel` | 📅 Upcoming |
| **2.2** | Audio Upload Ingestion & Magic-Byte Validation | `Catalog` | 📅 Upcoming |
| **2.3** | FFmpeg Background Transcoding Worker | `Streaming` | 📅 Upcoming |
| **2.5** | HLS Media Delivery & Redis Play Counting | `Streaming` | 📅 Upcoming |
| **3.0** | Social: Following, Activity Feeds & SignalR Notifications | `Social` | 📅 Planned |
| **4.0** | Analytics: Play Counts, Rollups & Artist Revenue | `Analytics` | 📅 Planned |

*For complete granular roadmap progression, see [`agent/ROADMAP.md`](agent/ROADMAP.md).*

---

## 🤝 Contributing

1. Fork the repository and create your branch: `git checkout -b feature/your-feature`
2. Commit your changes: `git commit -m "feat: add your feature"`
3. Push to your fork: `git push origin feature/your-feature`
4. Open a Pull Request

---

## 👤 Author

**Ahmad**  
[![GitHub](https://img.shields.io/badge/GitHub-100000?style=flat&logo=github&logoColor=white)](https://github.com/ahmads1990)

---

<div align="center">

**⭐ Star this repository if you find it helpful!**

Made with ❤️ using .NET 10

</div>