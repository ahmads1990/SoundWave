using Microsoft.Extensions.Logging;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;

namespace SoundWave.SharedKernel.Jobs;

internal class SendEmailJob : ISendEmailJob
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailJob> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendEmailJob"/> class.
    /// </summary>
    /// <param name="emailService">The email service used to send emails.</param>
    /// <param name="logger">The logger.</param>
    public SendEmailJob(IEmailService emailService, ILogger<SendEmailJob> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the email-sending job asynchronously.
    /// </summary>
    /// <param name="request">The email request parameters.</param>
    /// <param name="projectName">The name of the project to resolve template paths.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    public async Task Execute(EmailRequest request, string projectName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing SendEmailJob for {ToEmail} with subject {Subject}", request.ToEmail, request.Subject);
        await _emailService.SendEmailAsync(request, projectName, cancellationToken);
        _logger.LogInformation("SendEmailJob finished successfully for {ToEmail}", request.ToEmail);
    }
}
