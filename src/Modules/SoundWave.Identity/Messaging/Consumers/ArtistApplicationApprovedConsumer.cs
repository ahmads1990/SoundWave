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
/// Consumes the <see cref="ArtistApplicationApprovedEvent"/> integration event to upgrade
/// the user's role to <see cref="UserRole.Artist"/> and enqueue a congratulatory notification email.
/// </summary>
/// <param name="db">The Identity database context.</param>
/// <param name="backgroundJobClient">The Hangfire background job client for enqueuing email delivery.</param>
/// <param name="logger">The logger instance.</param>
internal sealed class ArtistApplicationApprovedConsumer(
    IdentityDbContext db,
    IBackgroundJobClient backgroundJobClient,
    ILogger<ArtistApplicationApprovedConsumer> logger)
    : IConsumer<ArtistApplicationApprovedEvent>
{
    /// <summary>
    /// Handles the artist application approved event by upgrading the user role and enqueuing the approval email.
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

        await UpgradeUserRoleToArtistAsync(user!, cancellationToken);
        EnqueueApprovalEmail(user!);
    }

    #region Private Methods

    /// <summary>
    /// Validates that the target user exists in the identity database.
    /// </summary>
    /// <param name="user">The retrieved user entity, or null if not found.</param>
    /// <param name="userId">The user identifier from the event payload.</param>
    /// <returns>True if the user exists; otherwise, false.</returns>
    private bool Validate(User? user, Guid userId)
    {
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found in Identity database for ArtistApplicationApprovedEvent", userId);
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
    /// Updates the user's role to Artist and persists changes to the database.
    /// </summary>
    /// <param name="user">The user entity to update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task UpgradeUserRoleToArtistAsync(User user, CancellationToken cancellationToken)
    {
        user.Role = UserRole.Artist;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} role upgraded to Artist following application approval", user.Id);
    }

    /// <summary>
    /// Constructs the email request and enqueues a Hangfire job to send the artist application approved notification.
    /// </summary>
    /// <param name="user">The approved user entity.</param>
    private void EnqueueApprovalEmail(User user)
    {
        var fullName = GetUserFullName(user);

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
