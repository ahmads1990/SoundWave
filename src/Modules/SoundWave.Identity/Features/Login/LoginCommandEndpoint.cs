using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Login;

/// <summary>
/// Exposes the HTTP endpoint for user authentication/login.
/// </summary>
internal class LoginCommandEndpoint : IEndpoint
{
    /// <summary>
    /// Configures the routing, filters, and OpenAPI documentation for the login endpoint.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/login", Handle)
           .AddEndpointFilter<ValidationFilter<LoginRequest>>()
           .WithTags(Constants.MODULE_TAG)
           .WithSummary("User login / authentication")
           .WithDescription("Authenticates a user using their registered email and password. Returns a JWT access token and a refresh token upon success.")
           .Produces<SuccessResponse<UserTokensDto>>(StatusCodes.Status200OK)
           .Produces<FailureResponse<UserTokensDto>>(StatusCodes.Status400BadRequest)
           .Produces<FailureResponse<UserTokensDto>>(StatusCodes.Status401Unauthorized)
           .Produces<FailureResponse<UserTokensDto>>(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Handles the incoming login request by converting it to a MediatR command.
    /// </summary>
    /// <param name="request">The login request payload.</param>
    /// <param name="sender">The MediatR sender instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An HTTP response with user tokens on success, or an error status code on failure.</returns>
    private static async Task<IResult> Handle(LoginRequest request, ISender sender, CancellationToken cancellationToken = default)
    {
        var command = request.Adapt<LoginCommand>();
        var result = await sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<UserTokensDto>(result.ApiErrorCode, result.Data!, result.ErrorMessage);
            return result.Error switch
            {
                IdentityError.InvalidCredentials => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
                IdentityError.EmailNotVerified => Results.BadRequest(response),
                _ => Results.InternalServerError(response)
            };
        }

        return Results.Ok(new SuccessResponse<UserTokensDto>(result.Data!));
    }
}
