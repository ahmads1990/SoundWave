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

namespace SoundWave.Identity.Features.VerifyEmail;

/// <summary>
/// Exposes the HTTP endpoint for verifying an email address.
/// </summary>
internal class VerifyEmailEndpoint : IEndpoint
{
    /// <summary>
    /// Configures the routing and filters for the verify email endpoint.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/verify-email", Handle)
            .AddEndpointFilter<ValidationFilter<VerifyEmailRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Verify email address")
            .WithDescription("Verifies the user's email address using the OTP sent to them.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Handles the incoming verify email request by converting it to a command and sending it via MediatR.
    /// </summary>
    private static async Task<IResult> Handle(VerifyEmailRequest request, ISender sender, CancellationToken ct = default)
    {
        var command = request.Adapt<VerifyEmailCommand>();
        var result = await sender.Send(command, ct);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<bool>(result.Error.ToApiErrorCode(), result.ErrorMessage);
            return result.Error switch
            {
                IdentityError.UserNotFound => Results.NotFound(response),
                _ => Results.BadRequest(response)
            };
        }

        return Results.Ok(new SuccessResponse<bool>(result.Data));
    }
}
