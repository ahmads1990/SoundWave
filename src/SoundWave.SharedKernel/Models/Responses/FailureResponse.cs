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
