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
    /// <param name="projectRootPath">The root path of the project's email templates directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendEmailAsync(EmailRequest request, string projectRootPath, CancellationToken cancellationToken = default);
}
