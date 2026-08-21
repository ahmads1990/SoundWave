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

namespace SoundWave.Catalog.Features.Artists.ListArtistAccountApprovals;

/// <summary>
/// Exposes the HTTP endpoint for administrators to list and filter artist account applications.
/// </summary>
internal class ListArtistAccountApprovalsQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/catalog/artists/applications", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()))
            .AddEndpointFilter<ValidationFilter<ListArtistAccountApprovalsRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("List artist account applications")
            .WithDescription("Retrieves a paginated list of artist account applications filtered by status. Restricted to admins.")
            .Produces<SuccessResponse<PaginatedResponse<ListArtistAccountApprovalDto>>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<PaginatedResponse<ListArtistAccountApprovalDto>>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<PaginatedResponse<ListArtistAccountApprovalDto>>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<PaginatedResponse<ListArtistAccountApprovalDto>>>(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> Handle(
        [AsParameters] ListArtistAccountApprovalsRequest request,
        ISender sender,
        CancellationToken cancellationToken = default)
    {
        var query = request.Adapt<ListArtistAccountApprovalsQuery>();
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<PaginatedResponse<ListArtistAccountApprovalDto>>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<PaginatedResponse<ListArtistAccountApprovalDto>>(result.Data!));
    }
}
