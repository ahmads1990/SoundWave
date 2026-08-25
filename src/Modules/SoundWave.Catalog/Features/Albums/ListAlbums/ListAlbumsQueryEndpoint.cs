using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Features.Albums.ListAlbums;

/// <summary>
/// Exposes the HTTP endpoint for listing albums with filters and pagination.
/// Public endpoint — no authentication required.
/// </summary>
internal class ListAlbumsQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/catalog/albums", Handle)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<ListAlbumsRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("List albums")
            .WithDescription($"Returns a paginated list of albums with optional filtering by title, genre, artist, publication status, and album type. Allowed album types: {EnumHelper.ToAllowedValuesString<AlbumType>()}. Allowed orderBy fields: {EnumHelper.FormatAllowedValues(nameof(Album.Title), nameof(Album.ReleaseDate), nameof(Album.TrackCount))}.")
            .Produces<SuccessResponse<PaginatedResponse<AlbumSummaryListDto>>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<PaginatedResponse<AlbumSummaryListDto>>>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Handle(
        [AsParameters] ListAlbumsRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var query = request.Adapt<ListAlbumsQuery>();
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<PaginatedResponse<AlbumSummaryListDto>>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<PaginatedResponse<AlbumSummaryListDto>>(result.Data!));
    }
}
