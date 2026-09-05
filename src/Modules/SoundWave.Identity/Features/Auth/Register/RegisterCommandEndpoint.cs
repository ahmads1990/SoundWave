using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

using SoundWave.Identity.Extensions;

namespace SoundWave.Identity.Features.Auth.Register;

/// <summary>
/// Exposes the HTTP endpoint for user registration.
/// </summary>
internal class RegisterCommandEndpoint : IEndpoint
{
    /// <summary>
    /// Configures the routing and filters for the registration endpoint.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("v1/auth/register", Handle)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .WithTags(Constants.Tags.Auth)
            .WithSummary("Register a new user account")
            .WithDescription("Creates a new listener user account and associated profile, hashes the password, and triggers the welcome email sequence.")
            .Produces<SuccessResponse<Guid>>(StatusCodes.Status201Created)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<Guid>>(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Handles the incoming registration request by converting it to a command and sending it via MediatR.
    /// </summary>
    /// <param name="request">The validated registration request.</param>
    /// <param name="sender">The MediatR sender to dispatch the command.</param>
    /// <param name="ct">Cancellation token for the asynchronous operation.</param>
    /// <returns>An HTTP result indicating success or failure, with appropriate status codes.</returns>
    private static async Task<IResult> Handle(RegisterRequest request, ISender sender, CancellationToken ct = default)
    {
        var command = request.Adapt<RegisterCommand>();
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<Guid>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                IdentityError.EmailAlreadyExists => Results.Conflict(response),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Created($"{result.Data}", new SuccessResponse<Guid>(result.Data));
    }
}
