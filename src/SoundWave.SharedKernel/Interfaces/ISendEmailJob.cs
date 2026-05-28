using SoundWave.SharedKernel.Models;

namespace SoundWave.SharedKernel.Interfaces;

/// <summary>
/// Defines the contract for background jobs responsible for sending emails.
/// </summary>
public interface ISendEmailJob
{
    /// <summary>
    /// Executes the email-sending job asynchronously.
    /// </summary>
    /// <param name="request">The email request parameters.</param>
    /// <param name="projectRootPath">The root path of the project's email templates directory.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task Execute(EmailRequest request, string projectRootPath, CancellationToken cancellationToken = default);
}