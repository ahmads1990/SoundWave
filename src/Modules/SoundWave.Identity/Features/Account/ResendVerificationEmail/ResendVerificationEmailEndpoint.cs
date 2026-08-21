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

namespace SoundWave.Identity.Features.Account.ResendVerificationEmail;

/// <summary>
/// Exposes the HTTP endpoint for resending the verification email.
/// </summary>
internal class ResendVerificationEmailEndpoint : IEndpoint
{
    /// <summary>
    /// Configures the routing and filters for the resend verification email endpoint.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/verify-email/resend", Handle)
            .AddEndpointFilter<ValidationFilter<ResendVerificationEmailRequest>>()
            .WithTags(Constants.MODULE_TAG)
            .WithSummary("Resend verification email")
            .WithDescription("Generates a new OTP and resends the verification email to the user.")
            .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
            .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<FailureResponse<bool>>(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Handles the incoming resend verification request by converting it to a command and sending it via MediatR.
    /// </summary>
    private static async Task<IResult> Handle(ResendVerificationEmailRequest request, ISender sender, CancellationToken ct = default)
    {
        var command = request.Adapt<ResendVerificationEmailCommand>();
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
