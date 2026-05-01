using Microsoft.AspNetCore.Http;
using SoundWave.SharedKernel.Interfaces;
using System.Security.Claims;

namespace SoundWave.SharedKernel.Services;

public class CurrentUserService : ICurrentUserService
{
    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;

    #endregion

    #region Constructors

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated
    {
        get
        {
            return _httpContextAccessor.HttpContext?
                       .User?
                       .Identity?
                       .IsAuthenticated ?? false;
        }
    }

    #endregion
}