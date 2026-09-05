using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Library.GetLibrary;

/// <summary>
/// Exposes the HTTP endpoint for retrieving the authenticated user's library items.
/// </summary>
internal class GetLibraryQueryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("v1/library", Handle)
            .RequireAuthorization()
            .WithTags(Constants.Tags.Library)
            .WithSummary("Get user's library")
            .WithDescription($"Retrieves aggregated library content (owned playlists, followed playlists, and saved albums). Allowed types: {EnumHelper.ToAllowedValuesString<LibraryItemTypeFilter>()}. Allowed sortBy: {EnumHelper.ToAllowedValuesString<LibrarySortBy>()}.")
            .Produces<SuccessResponse<IReadOnlyList<LibraryItemDto>>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<IReadOnlyList<LibraryItemDto>>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        LibraryItemTypeFilter type = LibraryItemTypeFilter.All,
        LibrarySortBy sortBy = LibrarySortBy.RecentlyAdded,
        ISender sender = default!,
        CancellationToken ct = default)
    {
        var query = new GetLibraryQuery(type, sortBy);
        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<IReadOnlyList<LibraryItemDto>>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<IReadOnlyList<LibraryItemDto>>(result.Data!));
    }
}
