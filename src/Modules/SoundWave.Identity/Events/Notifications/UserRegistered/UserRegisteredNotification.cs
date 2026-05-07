using MediatR;

namespace SoundWave.Identity.Events.Notifications.UserRegistered;

internal record UserRegisteredNotification(
    Guid UserId,
    string Email,
    string FullName) : INotification;
