using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;

namespace SoundWave.Identity.Events.Notifications.UserRegistered;

internal class SendWelcomeEmailHandler : INotificationHandler<UserRegisteredNotification>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(IBackgroundJobClient backgroundJobClient, ILogger<SendWelcomeEmailHandler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Sends welcome email to the user after registration using Hangfire background job. 
    /// </summary>
    /// <param name="notification"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

        _logger.LogInformation("Welcome email job enqueued for {ToEmail}, userId: {UserId}", notification.Email, notification.UserId);

        return Task.CompletedTask;
    }
}
