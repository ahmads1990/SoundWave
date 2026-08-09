using Hangfire;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Identity.Data;
using SoundWave.Identity.Data.Entites;
using SoundWave.SharedKernel.Common;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;
using Constants = SoundWave.Identity.Common.Constants;
using EmailTemplates = SoundWave.Identity.Common.EmailTemplates;

namespace SoundWave.Identity.Messaging.Consumers;

/// <summary>
/// Consumes the <see cref="ArtistApplicationRejectedEvent"/> integration event to enqueue
/// a rejection notification email containing the admin review reason.
/// </summary>
/// <param name="db">The Identity database context.</param>
/// <param name="backgroundJobClient">The Hangfire background job client for enqueuing email delivery.</param>
/// <param name="logger">The logger instance.</param>
internal sealed class ArtistApplicationRejectedConsumer(
    IdentityDbContext db,
    IBackgroundJobClient backgroundJobClient,
    ILogger<ArtistApplicationRejectedConsumer> logger)
    : IConsumer<ArtistApplicationRejectedEvent>
{
    /// <summary>
    /// Handles the artist application rejected event by validating the user and enqueuing the rejection email.
    /// </summary>
    /// <param name="context">The MassTransit consume context containing the rejected event payload.</param>
    /// <returns>A task representing the asynchronous consumption process.</returns>
    public async Task Consume(ConsumeContext<ArtistApplicationRejectedEvent> context)
    {
        var eventData = context.Message;
        var cancellationToken = context.CancellationToken;

        var user = await GetUserWithProfileAsync(eventData.UserId, cancellationToken);

        if (!Validate(user, eventData.UserId))
            return;

        EnqueueRejectionEmail(user!, eventData.RejectionReason);
    }

    #region Private Methods

    /// <summary>
    /// Validates that the rejected applicant user exists in the identity database.
    /// </summary>
    /// <param name="user">The retrieved user entity, or null if not found.</param>
    /// <param name="userId">The user identifier from the event payload.</param>
    /// <returns>True if the user exists; otherwise, false.</returns>
    private bool Validate(User? user, Guid userId)
    {
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found in Identity database for ArtistApplicationRejectedEvent", userId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Retrieves the user and associated user profile by unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user entity if found; otherwise, null.</returns>
    private Task<User?> GetUserWithProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        return db.Users
            .Include(u => u.UserProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <summary>
    /// Constructs the email request and enqueues a Hangfire job to send the artist application rejection notice.
    /// </summary>
    /// <param name="user">The rejected applicant user entity.</param>
    /// <param name="rejectionReason">The review reason explaining why the application was declined.</param>
    private void EnqueueRejectionEmail(User user, string rejectionReason)
    {
        var fullName = GetUserFullName(user);

        var request = new EmailRequest
        {
            ToName = fullName,
            ToEmail = user.Email,
            Subject = Constants.Email.Subjects.ArtistRejected,
            Template = EmailTemplates.ArtistApplicationRejected.ToString(),
            TemplateModel = new Dictionary<string, string>
            {
                { Constants.Email.TemplateKeys.FullName, fullName },
                { Constants.Email.TemplateKeys.Reason, rejectionReason },
                { Constants.Email.TemplateKeys.Year, DateTime.UtcNow.Year.ToString() }
            }
        };

        backgroundJobClient.Enqueue<ISendEmailJob>(job =>
            job.Execute(request, Constants.TEMPLATE_ROOT, default));

        logger.LogInformation("ArtistApplicationRejected email job enqueued for {ToEmail}, userId: {UserId}", user.Email, user.Id);
    }

    /// <summary>
    /// Resolves the user's full name from their profile or falls back to email.
    /// </summary>
    /// <param name="user">The user entity.</param>
    /// <returns>The resolved display or full name.</returns>
    private static string GetUserFullName(User user)
    {
        if (user.UserProfile is not null && !string.IsNullOrWhiteSpace(user.UserProfile.FirstName))
        {
            return $"{user.UserProfile.FirstName} {user.UserProfile.LastName}".Trim();
        }

        return user.Email;
    }

    #endregion
}
