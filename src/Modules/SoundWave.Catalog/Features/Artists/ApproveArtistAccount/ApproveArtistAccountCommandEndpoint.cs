using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Artists.ApproveArtistAccount;

/// <summary>
/// Exposes the HTTP endpoint for administrators to approve an artist account application.
/// Restricted to administrators only.
/// </summary>
internal class ApproveArtistAccountCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/catalog/artists/applications/{id:guid}/approve", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()))
            .WithTags(Constants.Tags.Artists)
            .WithSummary("Approve an artist account application")
            .WithDescription("Approves a pending artist application and creates the artist profile. Restricted to administrators.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new ApproveArtistAccountCommand(id);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.ArtistApplicationNotFound => Results.NotFound(response),
                CatalogError.UserNotAuthenticated       => Results.Unauthorized(),
                _                                       => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<Guid>(result.Data));
    }
}
