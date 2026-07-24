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

namespace SoundWave.Catalog.Features.RejectArtistAccount;

/// <summary>
/// Exposes the HTTP endpoint for administrators to reject an artist account application.
/// Restricted to administrators only.
/// </summary>
internal class RejectArtistAccountCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/catalog/artists/applications/{id:guid}/reject", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()))
            .AddEndpointFilter<ValidationFilter<RejectArtistAccountRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Reject an artist account application")
            .WithDescription("Rejects a pending artist application with a reason. Restricted to administrators.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        RejectArtistAccountRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new RejectArtistAccountCommand(id, request.Reason);
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
