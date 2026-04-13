using SoundWave.SharedKernel.Common.Attributes;

namespace SoundWave.SharedKernel.Models.Responses;

public enum ApiErrorCode
{
    [ErrorMessage("Success")]
    None = 0,

    // Authentication & Authorization (1000-1999)
    [ErrorMessage("Invalid username or password")]
    InvalidCredentials = 1001,

    [ErrorMessage("Please verify your email before logging in")]
    EmailNotVerified = 1002,

    [ErrorMessage("This email is already registered")]
    EmailAlreadyExists = 1003,

    [ErrorMessage("This username is already taken")]
    UsernameAlreadyExists = 1004,

    [ErrorMessage("Invalid or expired verification token")]
    InvalidVerificationToken = 1005,

    [ErrorMessage("Your session has expired. Please login again")]
    ExpiredToken = 1006,

    [ErrorMessage("You must be logged in to access this resource")]
    Unauthorized = 1007,

    [ErrorMessage("You don't have permission to access this resource")]
    Forbidden = 1008,

    [ErrorMessage("Invalid token")]
    InvalidToken = 1009,

    // Validation Errors (2000-2999)
    [ErrorMessage("Validation failed")]
    ValidationFailed = 2000,

    // Resource Errors (3000-3999)
    [ErrorMessage("Resource not found")]
    ResourceNotFound = 3000,

    // Business Logic Errors (4000-4999)

    // Server Errors (5000-5999)
    [ErrorMessage("An internal server error occurred")]
    InternalServerError = 5000,

    [ErrorMessage("A database error occurred")]
    DatabaseError = 5001,

    [ErrorMessage("Failed to send email")]
    EmailServiceError = 5002,

    [ErrorMessage("Cache service error")]
    CacheError = 5003
}
