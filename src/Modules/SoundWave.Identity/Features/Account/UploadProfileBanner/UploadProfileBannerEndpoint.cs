using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Identity.Common;
using SoundWave.Identity.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Account.UploadProfileBanner;

/// <summary>
/// Exposes the HTTP endpoint for uploading or updating the authenticated user's banner image.
/// </summary>
internal class UploadProfileBannerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/identity/users/me/banner", Handle)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags(Constants.Tags.Auth)
            .WithSummary("Upload user profile banner")
            .WithDescription("Uploads an image file (.jpg, .png, .webp) up to 5 MB as the authenticated user's cover banner.")
            .Produces<SuccessResponse<UploadImageResponse>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<UploadImageResponse>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        IFormFile? file,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new FailureResponse<UploadImageResponse>(
                ApiErrorCode.ValidationFailed, "An image file must be provided."));
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadProfileBannerCommand(userId, stream, file.FileName, file.Length);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<UploadImageResponse>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                IdentityError.UserNotFound => Results.NotFound(response),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<UploadImageResponse>(result.Data!));
    }
}
