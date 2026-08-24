# Plan 1 — Integration Testing with Real DB + Remove Generic Repository

## Context

SoundWave is a modular monolith (.NET, PostgreSQL) using vertical slice architecture.
Handlers call `DbContext` directly — there is no business logic layer to unit test in isolation.
The current codebase has a generic repository pattern (`IIdentityRepository<T>`) that adds complexity for no benefit.
This plan removes it and replaces the testing strategy with real DB + transaction rollback.

---

## Part A — Remove the Generic Repository

### What to delete

Delete every file matching these patterns:

```
SoundWave.Identity/
  Data/
    IRepository/
      IIdentityRepository.cs        ← delete
    Repository/
      IdentityRepository.cs         ← delete (or whatever it's called)
```

If other modules have the same pattern (`ICatalogRepository<T>`, etc.), delete those too.

### What replaces it

Nothing. Handlers inject `DbContext` directly.

**Before (what you have now):**

```csharp
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IIdentityRepository<User> _userRepository;
    private readonly IIdentityRepository<RefreshToken> _refreshTokenRepository;

    public LoginCommandHandler(
        IIdentityRepository<User> userRepository,
        IIdentityRepository<RefreshToken> refreshTokenRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await _userRepository.GetByEmail(command.Email, ct);
        // ...
        await _refreshTokenRepository.Add(refreshToken, ct);
        await _refreshTokenRepository.SaveChanges(ct);
    }
}
```

**After (what you want):**

```csharp
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IdentityDbContext _context;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(IdentityDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefresh = _tokenService.GenerateRawRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(rawRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedBy = user.Id,
            CreatedDate = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(ct);

        return Result.Success(new LoginResponse(accessToken, rawRefresh));
    }
}
```

### DI registration change

In `AuthModule.cs` (or wherever you register services), remove repository registrations:

```csharp
// DELETE these lines — they no longer exist
services.AddScoped<IIdentityRepository<User>, IdentityRepository<User>>();
services.AddScoped<IIdentityRepository<RefreshToken>, IdentityRepository<RefreshToken>>();

// DbContext registration stays as-is
services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("Identity")));
```

### Rule going forward

Every handler in every module injects its own module's `DbContext` directly.
No new repository interfaces or implementations are ever created for CRUD operations.

---

## Part B — Integration Testing Setup

### Project structure

Create one test project per module:

```
SoundWave.sln
  SoundWave.Identity/
  SoundWave.Catalog/
  ...
  tests/
    SoundWave.Identity.Tests/        ← create this
      Features/
        Auth/
          LoginCommandHandlerTests.cs
          RegisterCommandHandlerTests.cs
          RefreshTokenCommandHandlerTests.cs
      SoundWave.Identity.Tests.csproj
```

### NuGet packages for the test project

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
  <PackageReference Include="xunit" Version="2.7.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
  <ProjectReference Include="..\..\SoundWave.Identity\SoundWave.Identity.csproj" />
  <ProjectReference Include="..\..\SoundWave.SharedKernel\SoundWave.SharedKernel.csproj" />
</ItemGroup>
```

### Connection string for tests

Add `appsettings.Test.json` inside `tests/SoundWave.Identity.Tests/`:

```json
{
  "ConnectionStrings": {
    "Identity": "Host=localhost;Port=5432;Database=soundwave_test;Username=postgres;Password=yourpassword;"
  },
  "Jwt": {
    "Key": "test-secret-key-minimum-32-characters-long",
    "Issuer": "soundwave-test",
    "Audience": "soundwave-test",
    "AccessTokenLifeInMinutes": 15,
    "RefreshTokenLifeInDays": 7
  }
}
```

Add this to `.csproj` so the file is copied on build:

```xml
<ItemGroup>
  <Content Include="appsettings.Test.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Base test class (transaction rollback pattern)

Create `tests/SoundWave.Identity.Tests/IntegrationTestBase.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SoundWave.Identity.Data;
using SoundWave.SharedKernel.Configs;

namespace SoundWave.Identity.Tests;

// IAsyncLifetime = xUnit calls InitializeAsync before each test, DisposeAsync after
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IdentityDbContext DbContext { get; private set; } = null!;
    private IDbContextTransaction _transaction = null!;

    public async Task InitializeAsync()
    {
        // Read config from appsettings.Test.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        var connectionString = configuration.GetConnectionString("Identity")
            ?? throw new InvalidOperationException("Missing Identity connection string in appsettings.Test.json");

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        DbContext = new IdentityDbContext(options);

        // Run migrations so schema is always up to date
        // If schema already exists and is current, this is a no-op
        await DbContext.Database.MigrateAsync();

        // Start a transaction — everything written in the test will be rolled back
        _transaction = await DbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        // Roll back everything the test wrote — DB is exactly as it was before the test
        await _transaction.RollbackAsync();
        await DbContext.DisposeAsync();
    }

    /// <summary>
    /// Helper: seed entities directly into the DB within the current transaction.
    /// Use this to set up preconditions for a test.
    /// </summary>
    protected async Task SeedAsync<T>(params T[] entities) where T : class
    {
        DbContext.Set<T>().AddRange(entities);
        await DbContext.SaveChangesAsync();
    }
}
```

### How tests look

```csharp
// tests/SoundWave.Identity.Tests/Features/Auth/RegisterCommandHandlerTests.cs

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoundWave.Identity.Features.Auth.Register;
using SoundWave.Identity.Tests;
using SoundWave.SharedKernel.Configs;

namespace SoundWave.Identity.Tests.Features.Auth;

public class RegisterCommandHandlerTests : IntegrationTestBase
{
    private RegisterCommandHandler BuildHandler()
    {
        // Construct the handler exactly as DI would — no magic, no mocks
        var jwtConfig = Options.Create(new JwtConfig
        {
            Key = "test-secret-key-minimum-32-characters-long",
            Issuer = "soundwave-test",
            Audience = "soundwave-test",
            AccessTokenLifeInMinutes = 15,
            RefreshTokenLifeInDays = 7
        });

        var tokenService = new TokenService(jwtConfig);
        return new RegisterCommandHandler(DbContext, tokenService);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserInDb()
    {
        var handler = BuildHandler();
        var command = new RegisterCommand("ahmad@test.com", "StrongPass1!", "Ahmad");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var user = await DbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "ahmad@test.com");

        user.Should().NotBeNull();
        user!.DisplayName.Should().Be("Ahmad");
        // Password must be hashed — never stored as plain text
        user.PasswordHash.Should().NotBe("StrongPass1!");
        BCrypt.Net.BCrypt.Verify("StrongPass1!", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        // Seed an existing user with the same email
        await SeedAsync(new User
        {
            Id = Guid.CreateVersion7(),
            Email = "ahmad@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SomeOtherPass1!"),
            DisplayName = "Existing Ahmad",
            Role = Role.Listener,
            CreatedBy = Guid.Empty,
            CreatedDate = DateTime.UtcNow
        });

        var handler = BuildHandler();
        var command = new RegisterCommand("ahmad@test.com", "StrongPass1!", "Ahmad");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.EmailAlreadyExists");
    }

    [Fact]
    public async Task Handle_WeakPassword_ReturnsValidationError()
    {
        // This should be caught by the FluentValidation pipeline behaviour before
        // even reaching the handler — test the validator directly instead
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("ahmad@test.com", "weak", "Ahmad");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}

public class LoginCommandHandlerTests : IntegrationTestBase
{
    private LoginCommandHandler BuildHandler()
    {
        var jwtConfig = Options.Create(new JwtConfig
        {
            Key = "test-secret-key-minimum-32-characters-long",
            Issuer = "soundwave-test",
            Audience = "soundwave-test",
            AccessTokenLifeInMinutes = 15,
            RefreshTokenLifeInDays = 7
        });
        return new LoginCommandHandler(DbContext, new TokenService(jwtConfig));
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenPair()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "ahmad@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPass1!"),
            DisplayName = "Ahmad",
            Role = Role.Listener,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        });

        var handler = BuildHandler();
        var result = await handler.Handle(
            new LoginCommand("ahmad@test.com", "StrongPass1!"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify refresh token was persisted
        var savedToken = await DbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);
        savedToken.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        var userId = Guid.CreateVersion7();
        await SeedAsync(new User
        {
            Id = userId,
            Email = "ahmad@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass1!"),
            DisplayName = "Ahmad",
            Role = Role.Listener,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        });

        var handler = BuildHandler();
        var result = await handler.Handle(
            new LoginCommand("ahmad@test.com", "WrongPass1!"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ReturnsFailure()
    {
        var handler = BuildHandler();
        var result = await handler.Handle(
            new LoginCommand("ghost@test.com", "StrongPass1!"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }
}
```

---

## Part C — Ensuring Consistency

### How the transaction rollback works

```
Test 1 starts
  → InitializeAsync() called
  → MigrateAsync() runs (no-op if already current)
  → BeginTransactionAsync() — DB is now in a pending transaction

  Test runs:
    → SeedAsync(user)              writes user row (inside transaction, not committed)
    → handler.Handle(command)      reads + writes (all inside same transaction)
    → assertions pass or fail

  → DisposeAsync() called
  → RollbackAsync() — every row written during this test is gone
  → DB is identical to how it was before the test started

Test 2 starts with a clean DB
```

### Key rule — never call `SaveChangesAsync` with `useTransaction: false`

All EF Core saves inside a test automatically participate in the ambient transaction.
This is automatic — you do not need to do anything special in your handlers.

### Key rule — test data must be self-contained

Each test seeds exactly what it needs. No shared static seed data. No `[ClassFixture]` shared state.

```csharp
// WRONG — depends on data another test might have written
[Fact]
public async Task Handle_ReturnsExistingUser()
{
    // Assumes "ahmad@test.com" was seeded somewhere else — fragile
    var result = await handler.Handle(new LoginCommand("ahmad@test.com", "pass"), ct);
}

// CORRECT — seeds its own data
[Fact]
public async Task Handle_ReturnsExistingUser()
{
    await SeedAsync(new User { Email = "ahmad@test.com", ... });
    var result = await handler.Handle(new LoginCommand("ahmad@test.com", "pass"), ct);
}
```

### Key rule — parallel test execution

xUnit runs test *classes* in parallel by default but test *methods* within a class sequentially.
Since every class gets its own transaction, parallel class execution is safe — each class has its own `DbContext` instance and its own transaction.

If you ever see data leaking between tests, add this to the test project's `xunit.runner.json`:

```json
{
  "parallelizeTestCollections": false
}
```

---

## Checklist for the Coding Agent

Work through these in order. Do not skip steps.

- [ ] Delete `IIdentityRepository<T>` and its implementation
- [ ] Delete any other generic repository interfaces/implementations in other modules
- [ ] Update every handler that injected a repository to inject `DbContext` directly
- [ ] Remove repository DI registrations from all `*Module.cs` files
- [ ] Build solution — fix all compiler errors from the above deletions
- [ ] Create `tests/SoundWave.Identity.Tests/` project
- [ ] Add NuGet packages listed in Part B
- [ ] Add `appsettings.Test.json` with connection string pointing at a real PostgreSQL test DB
- [ ] Create `IntegrationTestBase.cs` exactly as shown
- [ ] Write handler tests for: `RegisterCommandHandler`, `LoginCommandHandler`, `RefreshTokenCommandHandler`
- [ ] Write validator unit tests for: `RegisterCommandValidator`, `LoginCommandValidator`
- [ ] Run `dotnet test` — all tests must pass
- [ ] Confirm no test leaves data in the DB (query the DB manually after a test run)
