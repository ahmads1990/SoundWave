using MediatR;

namespace SoundWave.Identity.Events.Notifications.PasswordResetRequested;

/// <summary>
/// Domain event notification dispatched when a user requests a password reset.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The registered email address.</param>
/// <param name="Otp">The password reset OTP generated for the user.</param>
internal record PasswordResetRequestedNotification(
    Guid UserId,
    string Email,
    string Otp) : INotification;
