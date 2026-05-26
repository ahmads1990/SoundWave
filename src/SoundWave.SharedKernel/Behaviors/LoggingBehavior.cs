using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SoundWave.SharedKernel.Behaviors;

/// <summary>
/// MediatR pipeline behavior that provides structured lifecycle logging for every command and query.
/// Measures elapsed time, captures request metadata, and logs business-level outcomes.
/// Ordered first in the pipeline so it wraps both validation and handler execution.
/// </summary>
/// <typeparam name="TRequest">The type of the MediatR request (command or query).</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Intercepts the pipeline, logs entry/exit with timing, and re-throws on exception.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms",requestName,stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Error handling {RequestName} after {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
