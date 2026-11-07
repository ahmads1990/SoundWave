# SoundWave Architectural & Workflow Diagrams

This directory contains interactive architectural and system flow diagrams for SoundWave, generated using [Archify](https://github.com/tt-a1i/archify).

Each `.html` file is a self-contained, interactive diagram with:
- Dark and Light mode themes
- Zoom, pan, and minimap navigation
- Flow motion / particle tracing
- Standalone export capabilities (SVG, PNG, WebM)

---

## 📊 Available Diagrams

| Diagram | Type | Interactive HTML File | Source Spec | Description |
| :--- | :--- | :--- | :--- | :--- |
| **High-Level System Architecture** | `architecture` | [system-architecture.html](file:///c:/Users/Ahmad/Projects/SoundWave/docs/diagrams/system-architecture.html) | [specs/system-architecture.json](file:///c:/Users/Ahmad/Projects/SoundWave/docs/diagrams/specs/system-architecture.json) | High-level topology of the SoundWave Modular Monolith: React SPA, ASP.NET Core Modules (Catalog, Identity, MediaProcessing, Web API), PostgreSQL databases, Redis cache, RabbitMQ event bus, and MinIO/S3 object storage. |
| **Direct-to-S3 Batch Track Ingestion Flow** | `sequence` | [batch-track-ingestion-sequence.html](file:///c:/Users/Ahmad/Projects/SoundWave/docs/diagrams/batch-track-ingestion-sequence.html) | [specs/batch-track-ingestion-sequence.json](file:///c:/Users/Ahmad/Projects/SoundWave/docs/diagrams/specs/batch-track-ingestion-sequence.json) | End-to-end multi-track ingestion: batch presigned URL generation, direct multipart upload to S3, batch track creation, domain events, and FFmpeg HLS transcoding orchestration. |
| **Multi-Module Relational ERD** | `erd` | [relational-erd.html](file:///c:/Users/Ahmad/Projects/SoundWave/docs/diagrams/relational-erd.html) | [specs/relational-erd.json](file:///c:/Users/Ahmad/Projects/SoundWave/docs/diagrams/specs/relational-erd.json) | Cross-module entity-relationship model linking Identity (`User`, `Role`), Catalog (`Artist`, `Album`, `Track`, `Playlist`), and MediaProcessing (`FileMetadata`). |
| **Track & FileMetadata State Machine** | `lifecycle` | [track-lifecycle.html](file:///c:/Users/Ahmad/Projects/SoundWave/docs/diagrams/track-lifecycle.html) | [specs/track-lifecycle.json](file:///c:/Users/Ahmad/Projects/SoundWave/docs/diagrams/specs/track-lifecycle.json) | State machine for media lifecycle (`PendingUpload` → `Uploaded` → `Processing` → `Ready` / `Failed`) alongside Track publication states (`Draft` → `Published` → `Archived`). |

---

## 🛠️ Re-rendering Diagrams

To re-render any diagram from its JSON source specification:

```pwsh
# High-Level Architecture
node .agents/skills/archify/bin/archify.mjs render architecture docs/diagrams/specs/system-architecture.json docs/diagrams/system-architecture.html

# Ingestion Sequence
node .agents/skills/archify/bin/archify.mjs render sequence docs/diagrams/specs/batch-track-ingestion-sequence.json docs/diagrams/batch-track-ingestion-sequence.html

# Relational ERD
node .agents/skills/archify/bin/archify.mjs render erd docs/diagrams/specs/relational-erd.json docs/diagrams/relational-erd.html

# State Lifecycle
node .agents/skills/archify/bin/archify.mjs render lifecycle docs/diagrams/specs/track-lifecycle.json docs/diagrams/track-lifecycle.html
```

