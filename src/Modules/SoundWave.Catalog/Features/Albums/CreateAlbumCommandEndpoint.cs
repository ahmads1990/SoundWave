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

namespace SoundWave.Catalog.Features.Albums.CreateAlbum;

/// <summary>
/// Exposes the HTTP endpoint for creating a new album.
/// Restricted to artists only.
/// </summary>
internal class CreateAlbumCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/catalog/albums", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Artist.ToString()))
            .AddEndpointFilter<ValidationFilter<CreateAlbumRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Create a new album")
            .WithDescription("Creates a new album for the authenticated artist. Albums start unpublished.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status201Created)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status403Forbidden)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        CreateAlbumRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = request.Adapt<CreateAlbumCommand>();
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                CatalogError.ArtistNotFound       => Results.NotFound(response),
                CatalogError.InvalidGenreId       => Results.BadRequest(response),
                CatalogError.UserNotAuthenticated => Results.Unauthorized(),
                _                                 => Results.BadRequest(response)
            };
        }

        return Results.Created($"/api/v1/catalog/albums/{result.Data}", new SuccessResponse<Guid>(result.Data));
    }
}
