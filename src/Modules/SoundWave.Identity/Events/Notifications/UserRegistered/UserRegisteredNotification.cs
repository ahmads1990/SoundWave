using MediatR;

namespace SoundWave.Identity.Events.Notifications.UserRegistered;

/// <summary>
/// Domain event notification dispatched after a new user registers in the identity module.
/// </summary>
/// <param name="UserId">The unique identifier of the registered user.</param>
/// <param name="Email">The registered email address.</param>
/// <param name="FullName">The user's full/display name.</param>
/// <param name="Otp">The email verification OTP generated for the user.</param>
internal record UserRegisteredNotification(
    Guid UserId,
    string Email,
    string FullName,
    string Otp) : INotification;
