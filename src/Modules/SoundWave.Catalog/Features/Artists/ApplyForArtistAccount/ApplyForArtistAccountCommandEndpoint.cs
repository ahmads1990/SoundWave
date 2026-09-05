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

namespace SoundWave.Catalog.Features.Artists.ApplyForArtistAccount;

/// <summary>
/// Exposes the HTTP endpoint for listeners to submit an artist account application.
/// Restricted to authenticated listeners.
/// </summary>
internal class ApplyForArtistAccountCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/catalog/artists/apply", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Listener.ToString()))
            .AddEndpointFilter<ValidationFilter<ApplyForArtistAccountRequest>>()
            .WithTags(Constants.Tags.Artists)
            .WithSummary("Apply for an artist account")
            .WithDescription("Submits a new application for an artist account. Restricted to listeners.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status201Created)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        ApplyForArtistAccountRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = request.Adapt<ApplyForArtistAccountCommand>();
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.ArtistApplicationAlreadyExists => Results.Conflict(response),
                CatalogError.UserNotAuthenticated          => Results.Unauthorized(),
                _                                          => Results.BadRequest(response)
            };
        }

        return Results.Created($"/api/v1/catalog/artists/applications/my", new SuccessResponse<Guid>(result.Data));
    }
}
