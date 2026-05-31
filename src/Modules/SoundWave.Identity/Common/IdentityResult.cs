using SoundWave.Identity.Extensions;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Common;

/// <summary>
/// Encapsulates the outcome of an identity operation, including success status, returned data, and errors.
/// </summary>
/// <typeparam name="T">The type of payload data returned by the operation.</typeparam>
internal class IdentityResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the payload data returned by the operation on success, or default/failure data on failure.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Gets the identity error code if the operation failed.
    /// </summary>
    public IdentityError Error { get; init; }

    /// <summary>
    /// Gets an optional descriptive message for the error if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the API-facing error code mapped from the internal <see cref="IdentityError"/>.
    /// </summary>
    public ApiErrorCode ApiErrorCode => Error.ToApiErrorCode();

    /// <summary>
    /// Creates a successful identity result containing the specified data.
    /// </summary>
    /// <param name="data">The payload data returned by the operation.</param>
    /// <returns>A successful <see cref="IdentityResult{T}"/> instance.</returns>
    public static IdentityResult<T> Success(T data) => new() { IsSuccess = true, Data = data, Error = IdentityError.None };

    /// <summary>
    /// Creates a failed identity result containing the specified error and optional message.
    /// </summary>
    /// <param name="error">The identity error code.</param>
    /// <param name="message">An optional error message.</param>
    /// <returns>A failed <see cref="IdentityResult{T}"/> instance.</returns>
    public static IdentityResult<T> Failure(IdentityError error, string? message = null)
        => new() { IsSuccess = false, Error = error, ErrorMessage = message };

    /// <summary>
    /// Creates a failed identity result containing the specified error, fallback data, and optional message.
    /// </summary>
    /// <param name="error">The identity error code.</param>
    /// <param name="data">The fallback payload data.</param>
    /// <param name="message">An optional error message.</param>
    /// <returns>A failed <see cref="IdentityResult{T}"/> instance.</returns>
    public static IdentityResult<T> Failure(IdentityError error, T data, string? message = null)
        => new() { IsSuccess = false, Error = error, Data = data, ErrorMessage = message };

    /// <summary>
    /// Converts a failed identity result to another type.
    /// </summary>
    public IdentityResult<TTarget> ToFailure<TTarget>()
    {
        if (IsSuccess) throw new InvalidOperationException("Cannot convert a successful result.");
        return IdentityResult<TTarget>.Failure(Error, ErrorMessage);
    }
}
