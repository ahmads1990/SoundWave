using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Identity.Common;
using SoundWave.Identity.Dtos;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

using SoundWave.Identity.Extensions;

namespace SoundWave.Identity.Features.Auth.RefreshTokens;

/// <summary>
/// Exposes the HTTP endpoint for refreshing session tokens.
/// </summary>
internal class RefreshTokensCommandEndpoint : IEndpoint
{
    /// <summary>
    /// Configures the routing, filters, and OpenAPI documentation for the refresh token endpoint.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/auth/refresh-tokens", Handle)
           .AddEndpointFilter<ValidationFilter<RefreshTokensRequest>>()
           .WithTags(Constants.Tags.Auth)
           .WithSummary("Refresh session credentials")
           .WithDescription("Accepts a valid refresh token and generates a new access token and refresh token pair.")
           .Produces<SuccessResponse<UserTokensDto>>(StatusCodes.Status200OK)
           .Produces<FailureResponse<UserTokensDto>>(StatusCodes.Status400BadRequest)
           .Produces<FailureResponse<UserTokensDto>>(StatusCodes.Status401Unauthorized)
           .Produces<FailureResponse<UserTokensDto>>(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Handles the incoming refresh token request by converting it to a MediatR command.
    /// </summary>
    /// <param name="request">The refresh token request payload.</param>
    /// <param name="sender">The MediatR sender instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An HTTP response with the new token pair, or an error status code on failure.</returns>
    private static async Task<IResult> Handle(RefreshTokensRequest request, ISender sender, CancellationToken cancellationToken = default)
    {
        var command = request.Adapt<RefreshTokensCommand>();
        var result = await sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<UserTokensDto>(result.Error.ToApiErrorCode(), result.Data!, result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<UserTokensDto>(result.Data!));
    }
}
