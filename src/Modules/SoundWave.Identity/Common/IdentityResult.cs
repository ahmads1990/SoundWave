using SoundWave.SharedKernel.Models.Responses;
using SoundWave.Identity.Extensions;

namespace SoundWave.Identity.Common;

internal class IdentityResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public IdentityError Error { get; init; }
    public string? ErrorMessage { get; init; }

    public ApiErrorCode ApiErrorCode => Error.ToApiErrorCode();


    public static IdentityResult<T> Success(T data) => new() { IsSuccess = true, Data = data, Error = IdentityError.None };
    
    public static IdentityResult<T> Failure(IdentityError error, string? message = null) 
        => new() { IsSuccess = false, Error = error, ErrorMessage = message };

    public static IdentityResult<T> Failure(IdentityError error, T data, string? message = null) 
        => new() { IsSuccess = false, Error = error, Data = data, ErrorMessage = message };
}
