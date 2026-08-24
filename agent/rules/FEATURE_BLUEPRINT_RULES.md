# Feature Blueprint Rules & Best Practices

All new features built in the SoundWave application must follow the structure, documentation, and validation design system demonstrated by the User Registration feature. 

---

## 1. Directory Structure (Vertical Slice Architecture)
Each feature must be located in its own self-contained directory under `Features/{FeatureName}/` in the respective module project.

A typical feature slice contains:
- `*Request.cs`: The external API request DTO (if it's an endpoint that accepts a request body).
- `*RequestValidator.cs`: Simple shape/format validation for the external request.
- `*CommandEndpoint.cs` or `*QueryEndpoint.cs`: Exposes the HTTP route, maps input to command/query, and calls MediatR.
- `*Command.cs` or `*Query.cs`: The internal CQRS request dispatch object.
- `*CommandHandler.cs` or `*QueryHandler.cs`: Orchestrates and executes the business logic. Contains private `Validate*Async()` methods for business validation.

---

## 2. API Contract Decoupling
API contracts (Requests/Responses) must be decoupled from internal MediatR Commands/Queries.
- Do **not** bind commands directly as request parameters.
- Accept a dedicated `*Request` record/class at the endpoint.
- Map the request DTO to the MediatR command (using Mapster via `request.Adapt<TCommand>()` or manual mapping) within the endpoint handler.

---

## 3. OpenAPI & Scalar Documentation

To ensure the auto-generated documentation via Scalar UI is descriptive and helpful, all endpoints must be annotated correctly.

### A. Endpoint Metadata
Endpoints mapped in the endpoint builder must include:
1. **Module Tag**: `.WithTags(Constants.MODULE_TAG)` (do not use magic string tags; always reference the module's tag constant).
2. **Operation Summary**: `.WithSummary("Short summary of what it does")`.
3. **Operation Description**: `.WithDescription("A detailed paragraph describing business rules, downstream consequences, and preconditions.")`.
4. **Produced Responses**: Explicitly declare all possible response models and status codes using `.Produces<TResponse>(StatusCodes.Status...)`.

Example:
```csharp
app.MapPost("api/v1/register", Handle)
    .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
    .WithTags(Constants.MODULE_TAG)
    .WithSummary("Register a new user account")
    .WithDescription("Creates a new listener user account and associated profile, hashes the password, and triggers the welcome email sequence.")
    .Produces<SuccessResponse<Guid>>(StatusCodes.Status201Created)
    .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
    .Produces<FailureResponse<Guid>>(StatusCodes.Status409Conflict);
```

### B. XML Documentation Comments
Every class, method, endpoint handler, and request record must include complete XML documentation comments (`///`).
- For **Primary Constructor Records** (e.g., `RegisterRequest`), add `<param>` elements documenting every parameter so they are extracted and displayed as field descriptions in the Scalar schema.

Example:
```csharp
/// <summary>
/// Represents the client-side request payload for user registration.
/// </summary>
/// <param name="Email">The unique email address of the user.</param>
/// <param name="Password">The plain-text password chosen by the user.</param>
internal record RegisterRequest(
    string Email,
    string Password);
```

---

## 4. Single-Layer Request Validation Strategy

Validation must be split into two distinct concerns to optimize execution and keep endpoint responses clean.

### Layer 1: Request Validation (Format & Shape)
- Implemented via `*RequestValidator` (inheriting from `AbstractValidator<TRequest>`).
- Validates properties that can be checked client-side without external dependencies (e.g., email format, required fields, string length, regex patterns).
- Executed on the HTTP pipeline via the `ValidationFilter<TRequest>` endpoint filter.
- Stops invalid payloads early before invoking MediatR.

### Layer 2: Business Validation (Database Constraints)
- Implemented inside the handler via private `ValidateAsync` methods.
- Validates business constraints that require database queries or external systems (e.g., checking if an email already exists in the database).
- The `Handle()` method calls `ValidateAsync()` first and short-circuits on failure.
- This pattern prevents interleaving validation and execution, allows finer error granularity, and simplifies the codebase by removing command validators.

### Visibility
- Keep validator classes `internal`.
- Ensure validators are registered automatically via `builder.Services.AddValidatorsFromAssembly(Assembly, includeInternalTypes: true)` inside the module configuration.

---

## 5. Repository & Testability Rules

Handlers must remain **trivially testable with Moq** — no custom async query providers, no `InMemoryDatabase` dependencies, no EF Core extension method workarounds.

### A. Handlers Must Not Build `IQueryable` Chains

**❌ Forbidden in handlers:**
```csharp
// Handler directly chains EF Core async extensions on IQueryable
var user = await userRepository.GetByCondition(u => u.Email == email)
                               .Select(u => new { u.Id, u.Name })
                               .FirstOrDefaultAsync(ct);
```

**✅ Required pattern — call a dedicated repository method:**
```csharp
// Handler calls a repository method that returns a concrete type
var user = await userRepository.GetUserLoginInfoByEmailAsync(email, ct);
```

### B. Repository Methods Must Return Concrete Types

All repository query methods must return `Task<T>`, `Task<T?>`, `Task<List<T>>`, or `Task<bool>` — **never `IQueryable<T>`** to the handler.

The EF Core query logic (`.Select()`, `.Where()`, `.Include()`, `.FirstOrDefaultAsync()`, etc.) must live inside the repository implementation, not in the handler.

### C. Why This Rule Exists

- `IQueryable` methods like `FirstOrDefaultAsync()` and `ToListAsync()` are **EF Core extension methods** that require `IAsyncQueryProvider` under the hood.
- Standard `List<T>.AsQueryable()` does **not** implement `IAsyncQueryProvider`, so mocking `GetByCondition()` in unit tests causes runtime exceptions.
- Moving queries into repository methods means tests only need `.ReturnsAsync(someDto)` — simple, reliable, no hacks.

### D. When `GetByCondition()` Is Acceptable

`GetByCondition()` from `IRepository<T>` may still be used **inside repository implementations** (they run against a real EF Core `DbSet` which has the correct query provider). It must **never** be called directly from a handler and then chained with async EF Core extensions.

### E. Naming Convention for Repository Query Methods

Use descriptive method names that express intent:
- `GetUserLoginInfoByEmailAsync(string email, ...)`
- `GetUserProfileByIdAsync(Guid userId, ...)`
- `GetActiveRefreshTokenByUserIdAsync(Guid userId, ...)`

Avoid generic names like `GetByEmail()` — the name should communicate **what shape of data** is being returned.

