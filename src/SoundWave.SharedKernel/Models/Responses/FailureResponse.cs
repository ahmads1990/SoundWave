namespace SoundWave.SharedKernel.Models.Responses;

/// <summary>
/// Represents a failure API response.
/// </summary>
/// <typeparam name="T">The type of the data returned.</typeparam>
public class FailureResponse<T> : BaseApiResponse<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailureResponse{T}"/> class. 
    /// </summary>
    /// <param name="errorCode">The error code representing the failure.</param>
    /// <param name="customMessage">An optional custom error message.</param>
    public FailureResponse(ApiErrorCode errorCode, string? customMessage = null)
    {
        Success = false;
        Data = default;
        ErrorCode = errorCode;
        Message = customMessage ?? errorCode.GetErrorMessage();
    }

    /// <summary>
    /// Initializes a new instance of the FailureResponse class with the specified error code, associated data, and an
    /// optional custom error message.
    /// </summary>
    /// <param name="errorCode">The error code that identifies the type of error that occurred.</param>
    /// <param name="data">The data associated with the failure response, providing additional context or information about the error.</param>
    /// <param name="customMessage">An optional custom message that describes the error in more detail. If not specified, a default message based on
    public FailureResponse(ApiErrorCode errorCode, T data, string? customMessage = null)
    {
        Success = false;
        Data = data;
        ErrorCode = errorCode;
        Message = customMessage ?? errorCode.GetErrorMessage();
    }

    /// <summary>
    /// Initializes a new instance with validation errors.
    /// </summary>
    public FailureResponse(ApiErrorCode errorCode, Dictionary<string, string[]> validationErrors)
    {
        Success = false;
        Data = default;
        ErrorCode = errorCode;
        Message = errorCode.GetErrorMessage();
        ValidationErrors = validationErrors;
    }
}
