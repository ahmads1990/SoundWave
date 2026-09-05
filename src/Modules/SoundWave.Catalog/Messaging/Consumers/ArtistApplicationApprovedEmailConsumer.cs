using Hangfire;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Contracts.IntegrationEvents;
using SoundWave.Catalog.Data;
using SoundWave.Catalog.Data.Entities.Lookups;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;

namespace SoundWave.Catalog.Messaging.Consumers;

/// <summary>
/// Consumes the <see cref="ArtistApplicationApprovedEvent"/> integration event to enqueue
/// a congratulatory notification email to the newly approved artist.
/// </summary>
/// <param name="db">The Catalog read-only database context for cross-module user lookups.</param>
/// <param name="backgroundJobClient">The Hangfire background job client for enqueuing email delivery.</param>
/// <param name="logger">The logger instance.</param>
internal sealed class ArtistApplicationApprovedEmailConsumer(
    CatalogReadDbContext db,
    IBackgroundJobClient backgroundJobClient,
    ILogger<ArtistApplicationApprovedEmailConsumer> logger)
    : IConsumer<ArtistApplicationApprovedEvent>
{
    /// <summary>
    /// Handles the artist application approved event by retrieving user details and enqueuing the approval email.
    /// </summary>
    /// <param name="context">The MassTransit consume context containing the approved event payload.</param>
    /// <returns>A task representing the asynchronous consumption process.</returns>
    public async Task Consume(ConsumeContext<ArtistApplicationApprovedEvent> context)
    {
        var eventData = context.Message;
        var cancellationToken = context.CancellationToken;

        var user = await GetUserWithProfileAsync(eventData.UserId, cancellationToken);

        if (!Validate(user, eventData.UserId))
            return;

        EnqueueApprovalEmail(user!);
    }

    #region Private Methods

    /// <summary>
    /// Validates that the target user exists in the cross-module Auth lookup.
    /// </summary>
    /// <param name="user">The retrieved user lookup entity, or null if not found.</param>
    /// <param name="userId">The user identifier from the event payload.</param>
    /// <returns>True if the user exists; otherwise, false.</returns>
    private bool Validate(UserLookup? user, Guid userId)
    {
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found in Auth schema for ArtistApplicationApprovedEvent", userId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Retrieves the user and associated user profile by unique identifier from the Auth schema.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user lookup entity if found; otherwise, null.</returns>
    private Task<UserLookup?> GetUserWithProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        return db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <summary>
    /// Constructs the email request and enqueues a Hangfire job to send the artist application approved notification.
    /// </summary>
    /// <param name="user">The approved user entity.</param>
    private void EnqueueApprovalEmail(UserLookup user)
    {
        var fullName = user.FullName;

        var request = new EmailRequest
        {
            ToName = fullName,
            ToEmail = user.Email,
            Subject = Constants.Email.Subjects.ArtistApproved,
            Template = EmailTemplates.ArtistApplicationApproved.ToString(),
            TemplateModel = new Dictionary<string, string>
            {
                { Constants.Email.TemplateKeys.FullName, fullName },
                { Constants.Email.TemplateKeys.Year, DateTime.UtcNow.Year.ToString() }
            }
        };

        backgroundJobClient.Enqueue<ISendEmailJob>(job =>
            job.Execute(request, Constants.TEMPLATE_ROOT, default));

        logger.LogInformation("ArtistApplicationApproved email job enqueued for {ToEmail}, userId: {UserId}", user.Email, user.Id);
    }

    #endregion
}
