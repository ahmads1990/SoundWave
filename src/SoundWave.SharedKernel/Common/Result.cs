using System;

namespace SoundWave.SharedKernel.Common;

/// <summary>
/// Encapsulates the outcome of an operation, including success status, returned data, and a generic error type.
/// </summary>
/// <typeparam name="TError">The type of the error code enum.</typeparam>
/// <typeparam name="TData">The type of payload data returned by the operation.</typeparam>
public class Result<TError, TData> where TError : struct, Enum
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the payload data returned by the operation on success, or default/failure data on failure.
    /// </summary>
    public TData? Data { get; init; }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public TError Error { get; init; }

    /// <summary>
    /// Gets an optional descriptive message for the error if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result containing the specified data.
    /// </summary>
    /// <param name="data">The payload data returned by the operation.</param>
    /// <returns>A successful <see cref="Result{TError, TData}"/> instance.</returns>
    public static Result<TError, TData> Success(TData data) 
        => new() { IsSuccess = true, Data = data, Error = default };

    /// <summary>
    /// Creates a failed result containing the specified error and optional message.
    /// </summary>
    /// <param name="error">The error code.</param>
    /// <param name="message">An optional error message.</param>
    /// <returns>A failed <see cref="Result{TError, TData}"/> instance.</returns>
    public static Result<TError, TData> Failure(TError error, string? message = null)
        => new() { IsSuccess = false, Error = error, ErrorMessage = message };

    /// <summary>
    /// Creates a failed result containing the specified error, fallback data, and optional message.
    /// </summary>
    /// <param name="error">The error code.</param>
    /// <param name="data">The fallback payload data.</param>
    /// <param name="message">An optional error message.</param>
    /// <returns>A failed <see cref="Result{TError, TData}"/> instance.</returns>
    public static Result<TError, TData> Failure(TError error, TData data, string? message = null)
        => new() { IsSuccess = false, Error = error, Data = data, ErrorMessage = message };

    /// <summary>
    /// Converts a failed result to another type.
    /// </summary>
    public Result<TError, TTarget> ToFailure<TTarget>()
    {
        if (IsSuccess) 
            throw new InvalidOperationException("Cannot convert a successful result.");
        return Result<TError, TTarget>.Failure(Error, ErrorMessage);
    }
}
