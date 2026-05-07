using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace SoundWave.SharedKernel.Filters;

public class ValidationFilter<TRequest> : IEndpointFilter
{
    private readonly IValidator<TRequest> _validator;

    public ValidationFilter(IValidator<TRequest> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
            return await next(context);

        var validation = await _validator.ValidateAsync(request);

        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        return await next(context);
    }
}
