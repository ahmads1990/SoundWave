# SoundWave — Project Overview Cache

## What Is SoundWave?
- Full Spotify clone
- .NET 8 API + React/Vite frontend
- Modular Monolith + CQRS + Vertical Slice Architecture
- 7 DB schemas: Identity, Catalog, Streaming, Playlist, Social, Analytics, Infra
- Event-driven via RabbitMQ (raw client, no MassTransit)
- HLS audio streaming via FFmpeg
- Local disk storage in dev (IFileStorage abstraction)
- JWT auth (15min access + 7d refresh)

## Tech Stack
- **Backend**: .NET 8, EF Core 8, MSSQL, MediatR, FluentValidation, Serilog+Seq, BCrypt, Redis, RabbitMQ.Client
- **Frontend**: React + Vite, React Router v6, TanStack Query, Axios, Tailwind CSS, hls.js
- **Search (Phase 5)**: ElasticSearch 8, Qdrant
- **Infra**: Docker Compose (MSSQL, Redis, RabbitMQ, Seq)
- **Testing**: xUnit + Moq, Testcontainers
- **IDs**: Guid.CreateVersion7() for entities, BIGINT IDENTITY for high-volume append-only

## Key Patterns
- Result<T> (no exceptions for business logic)
- Outbox pattern (DB + RabbitMQ)
- Repository per aggregate root
- Soft delete via EF Core global query filter
- No cross-schema FK constraints (app enforces)
- One DbContext per module

## Phases
- Phase 0: Study & Planning
- Phase 1: Foundation (Auth, Catalog, Playlists, Outbox, React shell)
- Phase 2: Streaming Pipeline (Upload, FFmpeg, HLS, Player)
- Phase 3: Social (Following, Notifications, Posts, Feed)
- Phase 4: Analytics (Rollup Worker, Dashboards)
- Phase 5: Search & Discovery (ES + Qdrant)
- Phase 6: Polish & Production Readiness

## Roles
- Guest: Browse, 30s preview
- Listener: Full streaming, playlists, likes, follows
- Artist: Upload, manage tracks/albums, analytics
- Admin: Approve artists, manage users, audit logs
