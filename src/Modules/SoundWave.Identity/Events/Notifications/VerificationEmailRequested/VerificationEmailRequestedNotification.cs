using MediatR;

namespace SoundWave.Identity.Events.Notifications.VerificationEmailRequested;

/// <summary>
/// Notification triggered when a user explicitly requests to resend their verification email.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="FullName">The user's full name.</param>
/// <param name="Otp">The one-time password for email verification.</param>
internal record VerificationEmailRequestedNotification(Guid UserId, string Email, string FullName, string Otp) : INotification;
