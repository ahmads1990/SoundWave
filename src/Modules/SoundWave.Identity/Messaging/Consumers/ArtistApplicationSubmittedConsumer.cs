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
/// Consumes the <see cref="ArtistApplicationSubmittedEvent"/> integration event to enqueue
/// an acknowledgement notification email to the applicant.
/// </summary>
/// <param name="db">The Identity database context.</param>
/// <param name="backgroundJobClient">The Hangfire background job client for enqueuing email delivery.</param>
/// <param name="logger">The logger instance.</param>
internal sealed class ArtistApplicationSubmittedConsumer(
    IdentityDbContext db,
    IBackgroundJobClient backgroundJobClient,
    ILogger<ArtistApplicationSubmittedConsumer> logger)
    : IConsumer<ArtistApplicationSubmittedEvent>
{
    /// <summary>
    /// Handles the artist application submitted event by validating the user and enqueuing the submission email.
    /// </summary>
    /// <param name="context">The MassTransit consume context containing the submitted event payload.</param>
    /// <returns>A task representing the asynchronous consumption process.</returns>
    public async Task Consume(ConsumeContext<ArtistApplicationSubmittedEvent> context)
    {
        var eventData = context.Message;
        var cancellationToken = context.CancellationToken;

        var user = await GetUserWithProfileAsync(eventData.UserId, cancellationToken);

        if (!Validate(user, eventData.UserId))
            return;

        EnqueueSubmissionEmail(user!);
    }

    #region Private Methods

    /// <summary>
    /// Validates that the applicant user exists in the identity database.
    /// </summary>
    /// <param name="user">The retrieved user entity, or null if not found.</param>
    /// <param name="userId">The user identifier from the event payload.</param>
    /// <returns>True if the user exists; otherwise, false.</returns>
    private bool Validate(User? user, Guid userId)
    {
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found in Identity database for ArtistApplicationSubmittedEvent", userId);
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
    /// Constructs the email request and enqueues a Hangfire job to send the artist application submission acknowledgement.
    /// </summary>
    /// <param name="user">The applicant user entity.</param>
    private void EnqueueSubmissionEmail(User user)
    {
        var fullName = GetUserFullName(user);

        var request = new EmailRequest
        {
            ToName = fullName,
            ToEmail = user.Email,
            Subject = Constants.Email.Subjects.ArtistSubmitted,
            Template = EmailTemplates.ArtistApplicationSubmitted.ToString(),
            TemplateModel = new Dictionary<string, string>
            {
                { Constants.Email.TemplateKeys.FullName, fullName },
                { Constants.Email.TemplateKeys.Year, DateTime.UtcNow.Year.ToString() }
            }
        };

        backgroundJobClient.Enqueue<ISendEmailJob>(job =>
            job.Execute(request, Constants.TEMPLATE_ROOT, default));

        logger.LogInformation("ArtistApplicationSubmitted email job enqueued for {ToEmail}, userId: {UserId}", user.Email, user.Id);
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
