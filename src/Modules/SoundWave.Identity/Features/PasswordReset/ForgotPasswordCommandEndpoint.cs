using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Filters;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.PasswordReset;

/// <summary>
/// Exposes the HTTP endpoint for initiating a password reset.
/// </summary>
internal class ForgotPasswordCommandEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/password/forgot", Handle)
           .AddEndpointFilter<ValidationFilter<ForgotPasswordRequest>>()
           .WithTags(Constants.MODULE_TAG)
           .WithSummary("Forgot password")
           .WithDescription("Initiates a password reset by generating an OTP and storing it.")
           .Produces<SuccessResponse<bool>>(StatusCodes.Status200OK)
           .Produces<FailureResponse<bool>>(StatusCodes.Status400BadRequest)
           .Produces<FailureResponse<bool>>(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> Handle(ForgotPasswordRequest request, ISender sender, CancellationToken cancellationToken = default)
    {
        var command = request.Adapt<ForgotPasswordCommand>();
        var result = await sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var response = new FailureResponse<bool>(result.ApiErrorCode, result.ErrorMessage);
            return Results.BadRequest(response);
        }

        return Results.Ok(new SuccessResponse<bool>(result.Data));
    }
}
