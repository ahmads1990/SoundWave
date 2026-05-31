using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Configs;

namespace SoundWave.Identity.Services;

internal sealed class TokenService : ITokenService
{
    private readonly JwtConfig _jwtConfig;
    private readonly IIdentityRepository<RefreshToken> _refreshTokenRepo;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IOptions<JwtConfig> jwtOptions,
        IIdentityRepository<RefreshToken> refreshTokenRepo,
        ILogger<TokenService> logger)
    {
        _jwtConfig = jwtOptions.Value;
        _refreshTokenRepo = refreshTokenRepo;
        _logger = logger;
    }

    public async Task<UserTokensDto> GenerateUserTokensAsync(UserLoginInfoDto user, Guid? previousTokenId = null, CancellationToken cancellationToken = default)
    {
        var jwtToken = GenerateAccessToken(user);
        var rawRefreshToken = GenerateRawRefreshToken();
        
        await SaveRefreshTokenAsync(user.Id, rawRefreshToken, previousTokenId, cancellationToken);
        
        return new UserTokensDto
        {
            JwtToken = jwtToken,
            RefreshToken = rawRefreshToken
        };
    }

    public bool VerifyToken(string rawToken, string storedHash)
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

    #region Private Methods

    private string GenerateAccessToken(UserLoginInfoDto user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role,            user.Role.ToString()),
            new Claim(ClaimTypes.Name,            user.Name),
            new Claim(ClaimTypes.Email,           user.Email),
            new Claim(CustomClaimTypes.Username,  user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _jwtConfig.Issuer,
            audience:           _jwtConfig.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(_jwtConfig.DurationInHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRawRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private string HashRefreshToken(string rawToken)
        => BCrypt.Net.BCrypt.HashPassword(rawToken);

    private async Task SaveRefreshTokenAsync(Guid userId, string rawToken, Guid? previousTokenId, CancellationToken cancellationToken)
    {
        if (previousTokenId.HasValue)
        {
            var previousToken = new RefreshToken
            {
                Id = previousTokenId.Value,
                RevokedAt = DateTime.UtcNow
            };
            _refreshTokenRepo.SaveInclude(previousToken, nameof(RefreshToken.RevokedAt));
        }

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = HashRefreshToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtConfig.RefreshTokenLifeInDays),
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        await _refreshTokenRepo.Add(refreshToken, cancellationToken);
        await _refreshTokenRepo.SaveChanges(cancellationToken);
    }

    #endregion
}
