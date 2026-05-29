using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Common;
using SoundWave.Identity.Events.Notifications.VerificationEmailRequested;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;

namespace SoundWave.Identity.Events.Notifications.UserRegistered;

/// <summary>
/// Handles sending the email verification OTP to a user.
/// Reacts to both new user registrations and explicit resend requests.
/// </summary>
internal class SendVerificationEmailHandler : 
    INotificationHandler<UserRegisteredNotification>,
    INotificationHandler<VerificationEmailRequestedNotification>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<SendVerificationEmailHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendVerificationEmailHandler"/> class.
    /// </summary>
    /// <param name="backgroundJobClient">The Hangfire background job client.</param>
    /// <param name="logger">The logger instance.</param>
    public SendVerificationEmailHandler(IBackgroundJobClient backgroundJobClient, ILogger<SendVerificationEmailHandler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Handles the notification by enqueuing a background job to send the verification OTP email.
    /// </summary>
    /// <param name="notification">The user registered notification payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the completion of enqueuing the job.</returns>
    public Task Handle(UserRegisteredNotification notification, CancellationToken cancellationToken = default)
    {
        EnqueueVerificationEmail(notification.Email, notification.FullName, notification.Otp, notification.UserId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the notification by enqueuing a background job to resend the verification OTP email.
    /// </summary>
    /// <param name="notification">The explicit email verification requested payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the completion of enqueuing the job.</returns>
    public Task Handle(VerificationEmailRequestedNotification notification, CancellationToken cancellationToken = default)
    {
        EnqueueVerificationEmail(notification.Email, notification.FullName, notification.Otp, notification.UserId);
        return Task.CompletedTask;
    }

    private void EnqueueVerificationEmail(string email, string fullName, string otp, Guid userId)
    {
        var request = new EmailRequest
        {
            ToName = fullName,
            ToEmail = email,
            Subject = Constants.Email.Subjects.VerifyEmail,
            Template = EmailTemplates.VerifyEmail.ToString(),
            TemplateModel = new Dictionary<string, string>
            {
                { Constants.Email.TemplateKeys.FullName, fullName },
                { Constants.Email.TemplateKeys.Otp, otp },
                { Constants.Email.TemplateKeys.Year, DateTime.Now.Year.ToString() }
            }
        };

        _backgroundJobClient.Enqueue<ISendEmailJob>(job =>
            job.Execute(request, Constants.TEMPLATE_ROOT, default)
        );

        _logger.LogInformation("Verification email job enqueued for {ToEmail}, userId: {UserId}", email, userId);
    }
}
