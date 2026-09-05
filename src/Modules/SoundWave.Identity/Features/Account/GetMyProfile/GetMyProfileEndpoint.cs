using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Identity.Common;
using SoundWave.Identity.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Account.GetMyProfile;

/// <summary>
/// Exposes the HTTP endpoint for retrieving the authenticated user's profile details.
/// </summary>
internal class GetMyProfileEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("v1/auth/profile/me", Handle)
            .RequireAuthorization()
            .WithTags(Constants.Tags.Auth)
            .WithSummary("Get current user profile")
            .WithDescription("Retrieves the authenticated user's profile details including images.")
            .Produces<SuccessResponse<UserProfileDto>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<UserProfileDto>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<UserProfileDto>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var query = new GetMyProfileQuery(userId);
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<UserProfileDto>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                IdentityError.UserNotFound => Results.NotFound(response),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<UserProfileDto>(result.Data!));
    }
}
