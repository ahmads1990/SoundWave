using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Dtos;
using SoundWave.SharedKernel.Configs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SoundWave.Identity.Helpers;

internal class TokenHelper : ITokenHelper
{
    #region Constants

    private const int OTP_LENGTH = 6;
    private const int REFRESH_TOKEN_LENGTH = 32;

    #endregion

    #region Fields

    private readonly string SECRET_KEY;
    private readonly JwtConfig _jwtConfig;
    private readonly IIdentityRepository<RefreshToken> _refreshTokenRepository;

    #endregion

    #region Constructors

    public TokenHelper(
        IOptions<JwtConfig> jwtOptions,
        IConfiguration configuration,
        IIdentityRepository<RefreshToken> refreshTokenRepository)
    {
        _jwtConfig = jwtOptions.Value;
        SECRET_KEY = configuration.GetSection("OTPSecretKey")?.Value
            ?? throw new InvalidOperationException("Missing required configuration: 'OTPSecretKey'.");
        _refreshTokenRepository = refreshTokenRepository;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public string GenerateJWT(UserTokenBaseClaims baseUserClaims, List<UserClaim> userClaims, int expiresInMinutes = 0)
    {
        // Validate base user claims
        if (baseUserClaims.AreClaimsInValid())
            return string.Empty;

        var jwtClaims = new UserClaim[]
        {
            new UserClaim(ClaimTypes.NameIdentifier, baseUserClaims.Uid.ToString()),
            new UserClaim(ClaimTypes.Role, baseUserClaims.Role.ToString()),
            new UserClaim(ClaimTypes.Name, baseUserClaims.Name ?? string.Empty),
            new UserClaim(ClaimTypes.Email, baseUserClaims.Email ?? string.Empty),
            new UserClaim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Merge base claims and additional user claims
        var allClaims = jwtClaims
            .Union(userClaims)
            .Select(c => new Claim(c.Type, c.Value));

        // Specify the signing key and algorithm
        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var expirationDate = expiresInMinutes > 0
            ? DateTime.UtcNow.AddMinutes(expiresInMinutes)
            : DateTime.UtcNow.AddHours(_jwtConfig.DurationInHours);

        // Create the JWT token
        var jwtSecurityToken = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: allClaims,
            expires: expirationDate,
            signingCredentials: signingCredentials
        );

        // Return the serialized token
        return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
    }

    /// <inheritdoc />
    public string GenerateOTP(int length = OTP_LENGTH)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than zero.", nameof(length));

        var sb = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            sb.Append(RandomNumberGenerator.GetInt32(0, 10));
        }
        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<string> GenerateAndSaveRefreshTokenAsync(Guid userId, Guid? tokenId = null, CancellationToken cancellationToken = default)
    {
        var refreshToken = GenerateRefreshToken();
        var hashedToken = BCrypt.Net.BCrypt.HashPassword(refreshToken);

        var entity = new RefreshToken
        {
            TokenHash = hashedToken,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtConfig.RefreshTokenLifeInDays),
        };

        if (tokenId.HasValue)
        {
            entity.Id = tokenId.Value;
            _refreshTokenRepository.SaveInclude(entity, nameof(RefreshToken.ExpiresAt), nameof(RefreshToken.TokenHash));
        }
        else
        {
            await _refreshTokenRepository.Add(entity, cancellationToken);
        }

        await _refreshTokenRepository.SaveChanges(cancellationToken);
        return refreshToken;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Generates a secure random refresh token.
    /// </summary>
    /// <returns>A base64 encoded refresh token string.</returns>
    private string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(REFRESH_TOKEN_LENGTH);
        return Convert.ToBase64String(randomBytes);
    }

    #endregion
}