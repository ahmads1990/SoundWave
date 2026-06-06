using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace SoundWave.API.Middlewares;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti))
            {
                var cachingService = context.RequestServices.GetRequiredService<ICachingService>();
                var cacheKey = SharedConstants.Caching.GetJwtBlacklistKey(jti);

                var isBlacklisted = await cachingService.GetAsync(cacheKey);
                if (!string.IsNullOrEmpty(isBlacklisted))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"Token has been revoked or logged out.\"}");
                    return;
                }
            }
        }

        await _next(context);
    }
}
