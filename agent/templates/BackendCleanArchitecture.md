# 🛠️ Backend Clean Architecture Template (.NET 8)
*Exact 1:1 structural mirror of `ExaminationSystemWebAPI` (stripped of multi-tenancy, flat root project layout)*

---

## 📂 Exact Solution & Directory Skeleton

```text
SolutionName.sln
│
├── SolutionName.Domain/
│   ├── Common/
│   │   ├── Constants.cs                      # Domain-wide constants
│   │   └── Enumeration.cs                    # Smart enum base class
│   ├── Entities/
│   │   ├── BaseModel.cs                      # Base entity: int ID, bool Deleted, CreatedDate, UpdatedDate
│   │   ├── AppUser.cs                        # Core user entity
│   │   ├── RefreshToken.cs                   # Refresh token rotation entity
│   │   └── [FeatureEntities...].cs           # Domain entities (e.g. Recipe, PantryItem, MealPlan)
│   ├── Interfaces/
│   │   └── IRepository.cs                    # Generic repository interface
│   └── SolutionName.Domain.csproj
│
├── SolutionName.Application/
│   ├── Common/
│   │   ├── Attributes/                       # Custom mapping & validation attributes
│   │   └── CustomClaimTypes.cs               # JWT claim keys (UserId, Email, Role)
│   ├── DTOs/
│   │   ├── BasePaginatedDto.cs               # PageIndex, PageSize, OrderBy, SortDirection
│   │   ├── Auth/
│   │   │   ├── LoginDto.cs
│   │   │   ├── RegisterDto.cs
│   │   │   └── UserTokensDto.cs
│   │   └── [FeatureDTOs...]/
│   ├── EmailTemplates/                       # HTML email template models/markup
│   │   ├── PasswordReset/
│   │   ├── ResendVerification/
│   │   └── Welcome/
│   ├── InfraInterfaces/
│   │   ├── ICachingService.cs                # Redis / memory cache abstraction
│   │   ├── IEmailService.cs                  # Email delivery contract
│   │   ├── IPasswordHelper.cs                # Password hashing / verification contract
│   │   └── ITokenHelper.cs                   # JWT token generation / principal validation contract
│   ├── Interfaces/
│   │   ├── IAuthService.cs                   # Auth business logic contract
│   │   └── I[Feature]Service.cs              # Feature business logic contracts
│   ├── Mappings/
│   │   └── MapsterConfig.cs                  # Mapster DTO <-> Entity mappings
│   ├── Services/
│   │   ├── AuthService.cs                    # Authentication service implementation
│   │   └── [FeatureServices...].cs           # Feature service implementations
│   ├── UseCases/                             # Complex domain orchestration workflows (optional)
│   ├── ServiceEnums.cs                       # UserOperationResult, SortingDirection, etc.
│   ├── ServiceExtensions.cs                  # AddApplicationServices() DI registration
│   └── SolutionName.Application.csproj
│
├── SolutionName.Infrastructure/
│   ├── Configs/
│   │   ├── JwtConfig.cs                      # Strongly-typed JWT options (Secret, Issuer, Audience, Expiry)
│   │   ├── RedisConfig.cs                    # Redis connection string & instance options
│   │   ├── SMTPConfig.cs                     # SMTP server, port, credentials
│   │   └── SystemServiceOptions.cs           # General system runtime configurations
│   ├── Data/
│   │   ├── Configurations/                   # IEntityTypeConfiguration<T> per entity
│   │   │   ├── AppUserConfiguration.cs
│   │   │   ├── RefreshTokenConfiguration.cs
│   │   │   └── [EntityConfigurations...].cs
│   │   ├── Migrations/                       # EF Core generated migration files
│   │   ├── Seeding/
│   │   │   └── AppDbSeeder.cs                # Initial database seeder
│   │   └── AppDbContext.cs                   # EF Core DbContext with auto-auditing & soft-delete filter
│   ├── Jobs/
│   │   └── JobRegistration.cs                # Hangfire recurring & background job registrations
│   ├── Services/
│   │   ├── Auth/
│   │   │   ├── PasswordHelper.cs             # BCrypt password hashing implementation
│   │   │   └── TokenHelper.cs                # JWT token creation, validation, JTI claim extraction
│   │   ├── Cache/
│   │   │   └── RedisCachingService.cs        # StackExchange.Redis caching implementation
│   │   └── Email/
│   │       └── EmailService.cs               # MailKit / SMTP email delivery implementation
│   ├── InfrastructureServiceExtensions.cs    # AddInfrastructureServices() DI registration
│   └── SolutionName.Infrastructure.csproj
│
├── SolutionName.API/
│   ├── Authorization/
│   │   ├── PolicyNames.cs                    # Custom policy name constants
│   │   ├── ScopeHandler.cs                   # AuthorizationHandler for custom scopes
│   │   └── ScopeRequirement.cs               # IAuthorizationRequirement for token scopes
│   ├── Common/
│   │   └── Constants.cs                      # API routing and header constants
│   ├── Controllers/
│   │   ├── BaseController.cs                 # Base [ApiController], [Route("api/[controller]")]
│   │   ├── AuthController.cs                 # Login, Register, Refresh, ForgotPassword, Logout
│   │   └── [FeatureControllers...].cs        # Feature endpoints
│   ├── Extensions/
│   │   ├── EnumExtensions.cs                 # Enum description and error code helper extensions
│   │   ├── ProgramExtensions.cs              # Clean Program.cs extension methods (Swagger, RateLimiting, JWT)
│   │   └── ServiceResultExtensions.cs        # Maps application ServiceEnums to HTTP responses
│   ├── Middlewares/
│   │   ├── GlobalExceptionHandlerMiddleware.cs # Catches unhandled exceptions, logs, returns ErrorResponse
│   │   ├── TokenBlacklistMiddleware.cs       # Validates token revocation against Redis blacklist
│   │   └── TransactionMiddleware.cs          # Wraps mutating HTTP requests (POST/PUT/DELETE) in EF transaction
│   ├── Models/
│   │   ├── Requests/
│   │   │   ├── Auth/
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   ├── RegisterRequest.cs
│   │   │   │   └── RefreshTokenRequest.cs
│   │   │   └── [FeatureRequests...]/
│   │   └── Responses/
│   │       ├── BaseResponse.cs               # Abstract ApiResponse<T>
│   │       ├── SuccessResponse.cs            # SuccessResponse<T> : ApiResponse<T>
│   │       ├── FailureResponse.cs            # ErrorResponse<T> : ApiResponse<T>
│   │       ├── PaginatedResponse.cs          # PaginatedResponse<T> : ApiResponse<IReadOnlyList<T>>
│   │       └── ErrorCode.cs                  # ApiErrorCode enum & message dictionary
│   ├── Templates/                            # API-level view/email templates
│   ├── Validators/
│   │   ├── Auth/
│   │   │   ├── LoginRequestValidator.cs
│   │   │   └── RegisterRequestValidator.cs
│   │   ├── BasePaginatedRequestValidator.cs
│   │   └── [FeatureValidators...]/
│   ├── appsettings.json                      # Connection strings, JWT, Redis, Serilog configuration
│   ├── appsettings.Development.json
│   ├── Program.cs                            # WebApplication builder & pipeline composition root
│   └── SolutionName.API.csproj
│
└── SolutionName.UnitTests/
    └── SolutionName.UnitTests.csproj         # xUnit unit testing project
```

---

## 🧱 Exact Core Code Patterns

### 1. `BaseModel.cs` (`Domain/Entities/BaseModel.cs`)

```csharp
namespace SolutionName.Domain.Entities;

public class BaseModel
{
    public int ID { get; set; }
    public bool Deleted { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
```

---

### 2. Base Paginated DTO (`Application/DTOs/BasePaginatedDto.cs`)

```csharp
namespace SolutionName.Application.DTOs;

public class BasePaginatedDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? OrderBy { get; set; }
    public SortingDirection SortDirection { get; set; } = SortingDirection.Ascending;
}
```

---

### 3. Unified API Response Envelope (`API/Models/Responses/`)

```csharp
// BaseResponse.cs
namespace SolutionName.API.Models.Responses;

public abstract class ApiResponse<T>
{
    public bool Success { get; set; } = false;
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public ApiErrorCode ErrorCode { get; set; } = ApiErrorCode.None;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// SuccessResponse.cs
public class SuccessResponse<T> : ApiResponse<T>
{
    public SuccessResponse(T data, string message = "Success")
    {
        Data = data;
        Success = true;
        Message = message;
        ErrorCode = ApiErrorCode.None;
    }
}

// FailureResponse.cs
public class ErrorResponse<T> : ApiResponse<T>
{
    public ErrorResponse(ApiErrorCode errorCode, string? customMessage = null)
    {
        Success = false;
        Data = default;
        ErrorCode = errorCode;
        Message = customMessage ?? errorCode.ToString();
    }
}

// PaginatedResponse.cs
public class PaginatedResponse<T> : ApiResponse<IReadOnlyList<T>>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public PaginatedResponse(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Data = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
        Success = true;
        Message = "Success";
        ErrorCode = ApiErrorCode.None;
    }
}
```

---

### 4. Base Controller (`API/Controllers/BaseController.cs`)

```csharp
namespace SolutionName.API.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
}
```

---

### 5. DbContext with Auto-Auditing & Global Soft-Delete (`Infrastructure/Data/AppDbContext.cs`)

```csharp
namespace SolutionName.Infrastructure.Data;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SolutionName.Domain.Entities;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global Soft-Delete Query Filter on all entities deriving from BaseModel
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseModel).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseModel.Deleted));
                var falseConstant = Expression.Constant(false);
                var compare = Expression.Equal(property, falseConstant);
                var lambda = Expression.Lambda(compare, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseModel>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedDate = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```
