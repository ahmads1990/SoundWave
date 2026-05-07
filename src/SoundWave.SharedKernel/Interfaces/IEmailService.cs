using SoundWave.SharedKernel.Models;

namespace SoundWave.SharedKernel.Interfaces;

/// <summary>
/// Defines the contract for sending emails using pre-defined templates.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously using a specific template.
    /// </summary>
    /// <param name="request">The email request parameters.</param>
    /// <param name="projectName">The name of the project to resolve template paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendEmailAsync(EmailRequest request, string projectName, CancellationToken cancellationToken = default);
}
