using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;

namespace SoundWave.Identity.Events.Notifications.PasswordResetRequested;

/// <summary>
/// Handles sending the password reset OTP to a user.
/// </summary>
internal class SendPasswordResetEmailHandler : INotificationHandler<PasswordResetRequestedNotification>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<SendPasswordResetEmailHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendPasswordResetEmailHandler"/> class.
    /// </summary>
    /// <param name="backgroundJobClient">The Hangfire background job client.</param>
    /// <param name="logger">The logger instance.</param>
    public SendPasswordResetEmailHandler(IBackgroundJobClient backgroundJobClient, ILogger<SendPasswordResetEmailHandler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Handles the notification by enqueuing a background job to send the password reset OTP email.
    /// </summary>
    /// <param name="notification">The password reset requested notification payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the completion of enqueuing the job.</returns>
    public Task Handle(PasswordResetRequestedNotification notification, CancellationToken cancellationToken = default)
    {
        var expiresIn = $"{Constants.Caching.UserPasswordResetTtlMinutes} minutes";

        var request = new EmailRequest
        {
            ToName = notification.Email, // We might not have the full name loaded in the handler easily, so we just use the email as name
            ToEmail = notification.Email,
            Subject = Constants.Email.Subjects.PasswordReset,
            Template = EmailTemplates.PasswordReset.ToString(),
            TemplateModel = new Dictionary<string, string>
            {
                { Constants.Email.TemplateKeys.Otp, notification.Otp },
                { Constants.Email.TemplateKeys.ExpiresIn, expiresIn },
                { Constants.Email.TemplateKeys.Year, DateTime.Now.Year.ToString() }
            }
        };

        _backgroundJobClient.Enqueue<ISendEmailJob>(job =>
            job.Execute(request, Constants.TEMPLATE_ROOT, default)
        );

        _logger.LogInformation("Password reset email job enqueued for {ToEmail}, userId: {UserId}", notification.Email, notification.UserId);

        return Task.CompletedTask;
    }
}
