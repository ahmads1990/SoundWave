using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Identity.Common;
using SoundWave.Identity.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Account.UpdateProfileImages;

/// <summary>
/// Exposes the HTTP endpoint for updating the authenticated user's profile and cover image URLs.
/// </summary>
internal class UpdateProfileImagesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("v1/auth/profile/images", Handle)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<UpdateProfileImagesRequest>>()
            .WithTags(Constants.Tags.Auth)
            .WithSummary("Update profile and cover images")
            .WithDescription("Updates the current authenticated user's profile picture and cover/banner image URLs.")
            .Produces<SuccessResponse<Unit>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<Unit>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Unit>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Unit>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        UpdateProfileImagesRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var command = new UpdateProfileImagesCommand(userId, request.ProfilePicUrl, request.CoverImageUrl);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Unit>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                IdentityError.UserNotFound => Results.NotFound(response),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<Unit>(result.Data));
    }
}
