using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using SoundWave.Identity.Extensions;

namespace SoundWave.Identity.Features.Auth.Logout;

/// <summary>
/// Exposes the HTTP endpoint for logging out a user.
/// </summary>
internal class LogoutCommandEndpoint : IEndpoint
{
    /// <summary>
    /// Configures the routing and filters for the logout endpoint.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/auth/logout", Handle)
            .RequireAuthorization()
            .WithTags(Constants.Tags.Auth)
            .WithSummary("User logout")
            .WithDescription("Revokes the user's refresh token and adds the JTI to the blacklist.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Handles the incoming logout request by converting it to a command and sending it via MediatR.
    /// </summary>
    private static async Task<IResult> Handle(ClaimsPrincipal user, ISender sender, CancellationToken ct = default)
    {
        var command = BuildCommand(user);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<bool>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                IdentityError.InvalidToken => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
                IdentityError.InvalidCredentials => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<bool>(result.Data));
    }

    private static LogoutCommand BuildCommand(ClaimsPrincipal user)
    {
        _ = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? string.Empty;

        DateTime? expiryDate = null;
        if (long.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Exp), out var expSeconds))
        {
            expiryDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
        }

        return new LogoutCommand(userId, jti, expiryDate);
    }
}
