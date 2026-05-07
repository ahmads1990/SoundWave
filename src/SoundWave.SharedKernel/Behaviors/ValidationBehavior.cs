using FluentValidation;
using MediatR;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.SharedKernel.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct = default)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
              validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            // Build a FailureResponse<T> matching TResponse's generic argument
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType)
            {
                var innerType = responseType.GetGenericArguments()[0];
                var failureType = typeof(FailureResponse<>).MakeGenericType(innerType);
                var response = Activator.CreateInstance(failureType, ApiErrorCode.ValidationFailed, errors);
                return (TResponse)response!;
            }

            // Fallback for non-generic TResponse
            throw new ValidationException(failures);
        }

        return await next();
    }
}
