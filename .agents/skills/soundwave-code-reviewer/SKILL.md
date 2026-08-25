---
name: soundwave-code-reviewer
description: >-
  Expert code reviewer and style enforcer for the SoundWave modular monolith backend.
  Use this skill whenever reviewing, refactoring, or writing new commands, queries, entities,
  handlers, endpoints, consumers, or tests in the SoundWave codebase to ensure strict adherence
  to architectural patterns and coding standards.
---

# SoundWave Code Review & Architectural Guidelines

This skill defines the architectural rules, coding standards, and review checklist for the **SoundWave** modular monolith codebase (`.NET 10`, `EF Core`, `MediatR`, `MassTransit`, `Minimal APIs`).

---

## 1. Command Handlers (Write Side)

- **Skinny Handlers Pattern**: Keep `Handle` methods concise:
  1. Validate prerequisites via a private `ValidateAsync` helper method.
  2. Execute mutation / entity creation logic.
  3. Save changes and return `Result<TError, TData>.Success(...)`.
- **Authentication & Authorization**:
  - If the endpoint has `.RequireAuthorization()`, do **NOT** check `if (userId is null)` with `UserNotAuthenticated` in the handler. Authentication is already guaranteed by middleware.
  - Access `currentUserService.UserId!.Value` directly.
- **Soft-Delete Guard**:
  - Never mutate or edit a soft-deleted entity. Always check `!entity.IsDeleted`. If deleted, report failure (`EntityNotFound` or specific error) and abort.
  - When performing a soft-delete: set `entity.IsDeleted = true`, `entity.UpdatedDate = DateTime.UtcNow`, `entity.UpdatedBy = userId`.
- **Sequential Positioning & Regapping**:
  - Appending items: set `Position = Parent.ItemCount + 1`.
  - Removing items: soft-delete the target item, shift subsequent active items (`Position > removedPosition && !pt.IsDeleted`) with `Position -= 1`, and decrement `Parent.ItemCount`.
  - Maintain denormalized counters (`TrackCount`, `FollowerCount`) within the same database transaction.

---

## 2. Query Handlers (Read Side)

- **Read Context & No-Tracking**:
  - Always use `*ReadDbContext` (e.g. `PlaylistReadDbContext`, `CatalogReadDbContext`) or `I*ReadRepository<T>`.
  - `QueryTrackingBehavior.NoTracking` is enforced by default.
  - Never call `SaveChangesAsync` on a read context.
- **Privacy & Security Filtering**:
  - For private resources (e.g., private playlists), verify ownership (`playlist.OwnerId == currentUserId`) or collaborator access. If unauthorized, return `PlaylistNotFound` (security through obscurity).
- **Pagination & Caching**:
  - Use `PaginatedResponse<T>` for paginated endpoints.
  - Cache public list results via `ICachingService` using centralized cache key helpers in `Constants.Caching`.

---

## 3. Minimal API Endpoints

- **Implementation**: Implement `IEndpoint` (from `SoundWave.SharedKernel.Common`).
- **Route Conventions**: Standard `api/v1/<module-resource>` URLs.
- **OpenAPI / Swagger**:
  - Tag endpoints with `Constants.MODULE_TAG`.
  - Provide `.WithSummary(...)` and `.WithDescription(...)`.
  - Declare `.Produces<SuccessResponse<T>>(...)` and `.Produces<FailureResponse<T>>(...)`.
- **Validation**: Apply `.AddEndpointFilter<ValidationFilter<TRequest>>()` from `SoundWave.SharedKernel.Filters` when request body validation is needed.
- **Response Mapping**:
  - On error: `new FailureResponse<T>(result.Error.ToApiErrorCode(), result.ErrorMessage)`.
  - Map errors to proper HTTP status codes: `NotFound`, `Unauthorized`, `BadRequest`, `Created`, `Ok`.

---

## 4. Logging & Diagnostics

- **Single-Line Logs**: Every log message (`ILogger<T>`) must be strictly on a single line.
- **Structured Properties**: Use named message template parameters (e.g. `logger.LogInformation("Playlist {PlaylistId} created by user {UserId}", playlist.Id, userId)`). Never use string interpolation in log templates.

---

## 5. Entities & EF Core Configurations

- **Individual Class Files**: Every entity class must reside in its own dedicated `.cs` file in `Data/Entities/` (e.g., `LikedTrack.cs`, `LikedAlbum.cs`, `LikedPlaylist.cs`, `PlaylistCollaborator.cs`). Never bundle multiple entity classes in a single file.
- **Configurations**: Entity mapping configurations must live in `Data/Configurations/` implementing `IEntityTypeConfiguration<TEntity>`.
- **Primary Keys & IDs**: Use sequential GUIDs generated via `Guid.CreateVersion7()`.

---

## 6. Messaging & Cross-Module Contracts

- **Contracts Project**: Integration events shared across modules must live in a dedicated `.Contracts` library (e.g., `SoundWave.Identity.Contracts`) as public records (e.g. `UserRegisteredEvent`).
- **Publishing**: Use MassTransit `IPublishEndpoint.Publish<T>` for cross-module integration events alongside internal MediatR notifications.
- **Consumers**: Implement `IConsumer<TEvent>` in `Messaging/Consumers/`, register via `Module.RegisterConsumers(x)` in `Program.cs`. Ensure idempotency in consumers.

---

## 7. Testing Standards

- **Test Base**: Inherit from `*IntegrationTestBase` (e.g., `PlaylistIntegrationTestBase`) using the shared transaction rollback pattern.
- **Coverage Requirement**:
  - Positive / happy path cases.
  - Validation failure cases (FluentValidation).
  - Not found / unauthorized cases.
  - System protected / boundary / idempotency cases.
- **Direct DbContext Assertions**: When asserting active records directly on DbContext sets, explicitly filter `.Where(e => !e.IsDeleted)`.

---

## Code Review Checklist

Before approving any PR or completing a feature, verify:

```markdown
- [ ] Handler Handle method is skinny and delegates validation to ValidateAsync.
- [ ] No redundant `if (userId is null)` checks in handlers for endpoints with `.RequireAuthorization()`.
- [ ] Soft-deleted entities cannot be edited or mutated; soft-delete sets `IsDeleted = true`, `UpdatedDate`, `UpdatedBy`.
- [ ] All `logger.Log*` calls are strictly single-line with structured template parameters.
- [ ] Read queries use `*ReadDbContext` without tracking and never call `SaveChangesAsync`.
- [ ] Every entity is in its own individual `.cs` file in `Data/Entities/`.
- [ ] Endpoints implement `IEndpoint`, use `Constants.MODULE_TAG`, and map response models cleanly.
- [ ] Cross-module events live in `*.Contracts` and consumers are idempotent.
- [ ] Unit and integration tests pass with 100% success across the solution (`dotnet test`).
```
