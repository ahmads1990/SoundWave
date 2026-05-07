using Hangfire;
using MediatR;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;

namespace SoundWave.Identity.Events.Notifications.UserRegistered;

internal class SendWelcomeEmailHandler : INotificationHandler<UserRegisteredNotification>
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public SendWelcomeEmailHandler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task Handle(UserRegisteredNotification notification, CancellationToken cancellationToken = default)
    {
        var request = new EmailRequest
        {
            ToName = notification.FullName,
            ToEmail = notification.Email,
            Subject = Constants.Email.Subjects.Welcome,
            Template = EmailTemplates.Welcome.ToString(),
            TemplateModel = new Dictionary<string, string>
            {
                { Constants.Email.TemplateKeys.FullName, notification.FullName },
                { Constants.Email.TemplateKeys.Year, DateTime.Now.Year.ToString() }
            }
        };

        _backgroundJobClient.Enqueue<ISendEmailJob>(job =>
            job.Execute(request, Constants.PROJECT_NAME, default)
        );
        return Task.CompletedTask;
    }
}
