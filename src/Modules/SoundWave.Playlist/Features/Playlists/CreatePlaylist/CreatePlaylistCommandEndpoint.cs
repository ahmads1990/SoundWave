using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Extensions;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Playlist.Features.Playlists.CreatePlaylist;

/// <summary>
/// Exposes the HTTP endpoint for creating a new playlist.
/// </summary>
internal class CreatePlaylistCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/playlists", Handle)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<CreatePlaylistRequest>>()
            .WithTags(Constants.Tags.Playlists)
            .WithSummary("Create a new playlist")
            .WithDescription("Creates a new custom playlist owned by the authenticated listener.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status201Created)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        CreatePlaylistRequest request,
        ISender sender,
        CancellationToken ct = default)
    {
        var command = new CreatePlaylistCommand(request.Title, request.Description, request.Visibility);
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                PlaylistError.UserNotAuthenticated => Results.Unauthorized(),
                _                                  => Results.BadRequest(response)
            };
        }

        return Results.Created($"/api/v1/playlists/{result.Data}", new SuccessResponse<Guid>(result.Data));
    }
}
