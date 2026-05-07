using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Register;

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
        app.MapPost("api/v1/register", Handle)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();
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

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                ApiErrorCode.EmailAlreadyExists => Results.Conflict(result),
                _ => Results.BadRequest(result)
            };
        }

        return Results.Created($"api/v1/users/{result.Data}", result);
    }
}
