# Plan 2 — Refactor TokenHelper into TokenService + OtpService

## Context

SoundWave Identity module has a `TokenHelper` class that does too much:
- Generates JWTs
- Generates OTPs (unrelated concern)
- Generates AND saves refresh tokens to the DB (mixes token logic with persistence)
- Returns `string.Empty` on invalid input instead of failing loudly

The `LoginCommandHandler` owns the token creation logic that `RefreshTokenCommandHandler` needs to duplicate.
This plan splits responsibilities cleanly so token logic is reusable across handlers with zero duplication.

---

## Part A — Files to Delete

```
SoundWave.Identity/
  Helpers/
    TokenHelper.cs         ← delete entirely
    ITokenHelper.cs        ← delete entirely
```

Remove any DI registration of `ITokenHelper` / `TokenHelper` in `AuthModule.cs`.

---

## Part B — Files to Create

### 1. `ITokenService.cs`

Location: `SoundWave.Identity/Services/ITokenService.cs`

```csharp
using SoundWave.Identity.Data.Entites;

namespace SoundWave.Identity.Services;

/// <summary>
/// Pure token operations — no I/O, no DB calls.
/// All persistence is the caller's (handler's) responsibility.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT access token from the user's identity.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically secure random refresh token.
    /// Returns the raw (unhashed) value — the caller hashes before saving.
    /// </summary>
    string GenerateRawRefreshToken();

    /// <summary>
    /// BCrypt-hashes a raw refresh token for safe DB storage.
    /// </summary>
    string HashRefreshToken(string rawToken);

    /// <summary>
    /// Verifies a raw refresh token against its stored BCrypt hash.
    /// </summary>
    bool VerifyRefreshToken(string rawToken, string storedHash);

    /// <summary>
    /// Reads claims from an expired JWT without throwing.
    /// Used in the refresh token flow to identify the user from an expired access token.
    /// Returns null if the token is structurally invalid (not just expired).
    /// </summary>
    ClaimsPrincipal? ReadExpiredToken(string accessToken);
}
```

### 2. `TokenService.cs`

Location: `SoundWave.Identity/Services/TokenService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SoundWave.Identity.Data.Entites;
using SoundWave.SharedKernel.Configs;

namespace SoundWave.Identity.Services;

internal sealed class TokenService : ITokenService
{
    private readonly JwtConfig _jwtConfig;

    public TokenService(IOptions<JwtConfig> jwtOptions)
    {
        _jwtConfig = jwtOptions.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role,            user.Role.ToString()),
            new Claim(ClaimTypes.Name,            user.DisplayName),
            new Claim(ClaimTypes.Email,           user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _jwtConfig.Issuer,
            audience:           _jwtConfig.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_jwtConfig.AccessTokenLifeInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRawRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string rawToken)
        => BCrypt.Net.BCrypt.HashPassword(rawToken);

    public bool VerifyRefreshToken(string rawToken, string storedHash)
        => BCrypt.Net.BCrypt.Verify(rawToken, storedHash);

    public ClaimsPrincipal? ReadExpiredToken(string accessToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key)),
            ValidateIssuer           = true,
            ValidIssuer              = _jwtConfig.Issuer,
            ValidateAudience         = true,
            ValidAudience            = _jwtConfig.Audience,
            // Critical: allow expired tokens in the refresh flow
            ValidateLifetime         = false
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(accessToken, validationParameters, out var validatedToken);

            // Reject tokens that aren't actually JWTs (structurally wrong)
            if (validatedToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
```

### 3. `IOtpService.cs`

Location: `SoundWave.Identity/Services/IOtpService.cs`

```csharp
namespace SoundWave.Identity.Services;

/// <summary>
/// Generates and verifies OTP codes for email verification and password reset.
/// OTPs are stored in Redis, not in the DB — this service only deals with the code itself.
/// </summary>
public interface IOtpService
{
    /// <summary>Generates a numeric OTP of the given length (default 6 digits).</summary>
    string GenerateOtp(int length = 6);
}
```

### 4. `OtpService.cs`

Location: `SoundWave.Identity/Services/OtpService.cs`

```csharp
using System.Security.Cryptography;
using System.Text;

namespace SoundWave.Identity.Services;

internal sealed class OtpService : IOtpService
{
    public string GenerateOtp(int length = 6)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than zero.", nameof(length));

        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(RandomNumberGenerator.GetInt32(0, 10));

        return sb.ToString();
    }
}
```

---

## Part C — DI Registration

In `AuthModule.cs` (the static extension method that registers Identity services):

```csharp
// Remove:
services.AddScoped<ITokenHelper, TokenHelper>();

// Add:
services.AddScoped<ITokenService, TokenService>();
services.AddScoped<IOtpService, OtpService>();
```

---

## Part D — Refactor Handlers

### LoginCommandHandler

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoundWave.Identity.Data;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel;

namespace SoundWave.Identity.Features.Auth.Login;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
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

        // Return the same error for "not found" and "wrong password"
        // Never leak which one it was
        if (user is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);

        if (user.IsLocked)
            return Result.Failure<LoginResponse>(AuthErrors.AccountLocked);

        return await IssueTokenPairAsync(user, ct);
    }

    // This private method is what LoginCommandHandler and RefreshTokenCommandHandler
    // both use — but it lives HERE, so they share the ITokenService, not each other
    private async Task<Result<LoginResponse>> IssueTokenPairAsync(User user, CancellationToken ct)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefresh  = _tokenService.GenerateRawRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id          = Guid.CreateVersion7(),
            UserId      = user.Id,
            TokenHash   = _tokenService.HashRefreshToken(rawRefresh),
            ExpiresAt   = DateTime.UtcNow.AddDays(7),
            CreatedBy   = user.Id,
            CreatedDate = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(ct);

        return Result.Success(new LoginResponse(accessToken, rawRefresh));
    }
}
```

### RefreshTokenCommandHandler

Notice: same `ITokenService` injection, same token generation pattern — no logic duplication.

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoundWave.Identity.Data;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel;

namespace SoundWave.Identity.Features.Auth.RefreshToken;

internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IdentityDbContext _context;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(IdentityDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        // Find the stored refresh token
        var stored = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.UserId == command.UserId &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTime.UtcNow, ct);

        if (stored is null)
            return Result.Failure<LoginResponse>(AuthErrors.InvalidRefreshToken);

        // Verify the raw token against the stored hash
        if (!_tokenService.VerifyRefreshToken(command.RawRefreshToken, stored.TokenHash))
            return Result.Failure<LoginResponse>(AuthErrors.InvalidRefreshToken);

        // Revoke the old token
        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByTokenId = Guid.CreateVersion7(); // will be set after new token is created

        // Issue a new token pair — same pattern as LoginCommandHandler
        var accessToken = _tokenService.GenerateAccessToken(stored.User);
        var rawRefresh  = _tokenService.GenerateRawRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            Id                = stored.ReplacedByTokenId!.Value,
            UserId            = stored.UserId,
            TokenHash         = _tokenService.HashRefreshToken(rawRefresh),
            ExpiresAt         = DateTime.UtcNow.AddDays(7),
            CreatedBy         = stored.UserId,
            CreatedDate       = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(ct);

        return Result.Success(new LoginResponse(accessToken, rawRefresh));
    }
}
```

### ForgotPasswordCommandHandler (uses OtpService, not TokenService)

```csharp
using MediatR;
using SoundWave.Identity.Data;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;

namespace SoundWave.Identity.Features.Auth.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IdentityDbContext _context;
    private readonly IOtpService _otpService;
    private readonly IDatabase _redis;

    public ForgotPasswordCommandHandler(
        IdentityDbContext context,
        IOtpService otpService,
        IDatabase redis)
    {
        _context    = context;
        _otpService = otpService;
        _redis      = redis;
    }

    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email, ct);

        // Always return success — never leak whether the email exists
        if (user is null)
            return Result.Success();

        var otp = _otpService.GenerateOtp();

        // Store in Redis: pwd_reset:{userId} with 1 hour TTL
        await _redis.StringSetAsync(
            $"pwd_reset:{user.Id}",
            otp,
            TimeSpan.FromHours(1));

        // TODO: send email (Phase 3 — for now just log it)
        // _logger.LogInformation("Password reset OTP for {UserId}: {Otp}", user.Id, otp);

        return Result.Success();
    }
}
```

---

## Part E — The Shared Logic Rule

This is the architectural rule to follow for every future handler:

```
Question: I need logic that two handlers both use. Where does it go?

Answer:

  Is it token generation/verification?   → ITokenService
  Is it OTP generation?                  → IOtpService
  Is it email sending?                   → IEmailService (when you build it)
  Is it file storage?                    → IFileStorage (already planned)
  Is it something only one handler needs? → Private method on that handler, nowhere else

NEVER call one handler from another handler.
NEVER put shared handler logic in a static class.
NEVER put DB calls inside TokenService or OtpService.
```

---

## Part F — Unit Tests for the New Services

These are pure unit tests — no DB, no mocks, no EF Core.
Create `tests/SoundWave.Identity.Tests/Services/TokenServiceTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Services;
using SoundWave.SharedKernel.Configs;

namespace SoundWave.Identity.Tests.Services;

public class TokenServiceTests
{
    // Shared config — 32+ character key required for HmacSha256
    private static readonly JwtConfig TestConfig = new()
    {
        Key                    = "test-secret-key-minimum-32-chars!!",
        Issuer                 = "soundwave-test",
        Audience               = "soundwave-test",
        AccessTokenLifeInMinutes = 15,
        RefreshTokenLifeInDays = 7
    };

    private static TokenService BuildService()
        => new TokenService(Options.Create(TestConfig));

    private static User BuildUser() => new()
    {
        Id          = Guid.NewGuid(),
        Email       = "ahmad@test.com",
        DisplayName = "Ahmad",
        Role        = Role.Listener,
        PasswordHash = "irrelevant-for-token-tests"
    };

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsNonEmptyString()
    {
        var svc   = BuildService();
        var token = svc.GenerateAccessToken(BuildUser());
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        var user  = BuildUser();
        var svc   = BuildService();
        var token = svc.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ExpiresInConfiguredMinutes()
    {
        var svc   = BuildService();
        var before = DateTime.UtcNow;
        var token = svc.GenerateAccessToken(BuildUser());
        var after  = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeAfter(before.AddMinutes(TestConfig.AccessTokenLifeInMinutes - 1));
        jwt.ValidTo.Should().BeBefore(after.AddMinutes(TestConfig.AccessTokenLifeInMinutes + 1));
    }

    [Fact]
    public void GenerateRawRefreshToken_ReturnsDifferentValuesEachCall()
    {
        var svc    = BuildService();
        var token1 = svc.GenerateRawRefreshToken();
        var token2 = svc.GenerateRawRefreshToken();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void HashAndVerify_RoundTrip_Succeeds()
    {
        var svc  = BuildService();
        var raw  = svc.GenerateRawRefreshToken();
        var hash = svc.HashRefreshToken(raw);

        svc.VerifyRefreshToken(raw, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyRefreshToken_WrongRaw_ReturnsFalse()
    {
        var svc  = BuildService();
        var raw  = svc.GenerateRawRefreshToken();
        var hash = svc.HashRefreshToken(raw);

        svc.VerifyRefreshToken("wrong-token", hash).Should().BeFalse();
    }

    [Fact]
    public void ReadExpiredToken_ExpiredToken_ReturnsPrincipal()
    {
        // To test this properly we need a token config with 0 minute lifetime
        var expiredConfig = new JwtConfig
        {
            Key                      = TestConfig.Key,
            Issuer                   = TestConfig.Issuer,
            Audience                 = TestConfig.Audience,
            AccessTokenLifeInMinutes = -1, // already expired
            RefreshTokenLifeInDays   = 7
        };

        var svc       = new TokenService(Options.Create(expiredConfig));
        var user      = BuildUser();
        var token     = svc.GenerateAccessToken(user);
        var principal = svc.ReadExpiredToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value
            .Should().Be(user.Id.ToString());
    }

    [Fact]
    public void ReadExpiredToken_GarbageToken_ReturnsNull()
    {
        var svc       = BuildService();
        var principal = svc.ReadExpiredToken("this.is.garbage");
        principal.Should().BeNull();
    }
}

public class OtpServiceTests
{
    private static OtpService BuildService() => new OtpService();

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void GenerateOtp_ReturnsCorrectLength(int length)
    {
        var otp = BuildService().GenerateOtp(length);
        otp.Should().HaveLength(length);
    }

    [Fact]
    public void GenerateOtp_ContainsOnlyDigits()
    {
        var otp = BuildService().GenerateOtp(6);
        otp.Should().MatchRegex(@"^\d+$");
    }

    [Fact]
    public void GenerateOtp_ReturnsDifferentValuesEachCall()
    {
        var svc  = BuildService();
        var otp1 = svc.GenerateOtp();
        var otp2 = svc.GenerateOtp();
        // This could theoretically collide 1 in 1,000,000 times — acceptable
        otp1.Should().NotBe(otp2);
    }

    [Fact]
    public void GenerateOtp_ZeroLength_Throws()
    {
        var act = () => BuildService().GenerateOtp(0);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*greater than zero*");
    }
}
```

---

## Checklist for the Coding Agent

Work through these in order. Do not skip steps.

- [ ] Delete `TokenHelper.cs` and `ITokenHelper.cs`
- [ ] Create `Services/ITokenService.cs` exactly as shown in Part B
- [ ] Create `Services/TokenService.cs` exactly as shown in Part B
- [ ] Create `Services/IOtpService.cs` exactly as shown in Part B
- [ ] Create `Services/OtpService.cs` exactly as shown in Part B
- [ ] Update `AuthModule.cs`: remove `ITokenHelper` registration, add `ITokenService` and `IOtpService`
- [ ] Refactor `LoginCommandHandler` to inject `IdentityDbContext` + `ITokenService` only (no repo, no TokenHelper)
- [ ] Refactor `RefreshTokenCommandHandler` to inject `IdentityDbContext` + `ITokenService` only
- [ ] Refactor `ForgotPasswordCommandHandler` to inject `IdentityDbContext` + `IOtpService` + `IDatabase`
- [ ] Remove the `OTPSecretKey` config dependency that was only used by `TokenHelper` (it's no longer needed since OTP is just a random number, not HMAC-based)
- [ ] Build solution — zero compiler errors
- [ ] Create `tests/SoundWave.Identity.Tests/Services/TokenServiceTests.cs` as shown in Part F
- [ ] Create `tests/SoundWave.Identity.Tests/Services/OtpServiceTests.cs` as shown in Part F
- [ ] Run `dotnet test` — all tests must pass
