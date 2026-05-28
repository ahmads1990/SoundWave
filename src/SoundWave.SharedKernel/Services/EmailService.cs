using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SoundWave.SharedKernel.Configs;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.SharedKernel.Models;
using System.Text.RegularExpressions;

namespace SoundWave.SharedKernel.Services;

/// <summary>
/// Provides functionality for sending emails using SMTP with support for HTML and plain text templates.
/// </summary>
public class EmailService : IEmailService
{
    #region Constants

    private const string EmailTemplatesFolder = "EmailTemplates";

    #endregion

    #region Fields

    private readonly SMTPConfig _smtpConfig;
    private readonly ILogger<EmailService> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="smtpConfig">The SMTP configuration settings.</param>
    /// <param name="logger">Logger instance.</param>
    public EmailService(IOptions<SMTPConfig> smtpConfig, ILogger<EmailService> logger)
    {
        _smtpConfig = smtpConfig.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public async Task SendEmailAsync(EmailRequest request, string projectRootPath, CancellationToken cancellationToken = default)
    {
        var templateBasePath = BuildTemplatePath(projectRootPath);
        ValidateEmailParameters(request.ToEmail, request.Subject);

        var message = await CreateEmailMessage(request, templateBasePath);
        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_smtpConfig.Host, _smtpConfig.Port, _smtpConfig.EnableSsl, cancellationToken);

            if (!string.IsNullOrEmpty(_smtpConfig.Username) && !string.IsNullOrEmpty(_smtpConfig.Password))
            {
                await client.AuthenticateAsync(_smtpConfig.Username, _smtpConfig.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {ToEmail} regarding {Subject}", request.ToEmail, request.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", request.ToEmail);
            throw new InvalidOperationException("Failed to send email.", ex);
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true, cancellationToken);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns the provided module's email template root path.
    /// </summary>
    /// <param name="moduleRootPath">The directory containing the module's email templates.</param>
    private static string BuildTemplatePath(string moduleRootPath)
    {
        if (string.IsNullOrWhiteSpace(moduleRootPath))
            throw new ArgumentException("Module template root path must be provided for email template resolution.", nameof(moduleRootPath));

        return Path.Combine(moduleRootPath, EmailTemplatesFolder);
    }

    /// <summary>
    /// Validates that essential email parameters are provided.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    private static void ValidateEmailParameters(string toEmail, string subject)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject is required.", nameof(subject));
    }

    /// <summary>
    /// Creates a new email message based on the provided parameters and template.
    /// </summary>
    /// <param name="request">The email request.</param>
    /// <param name="templateBasePath">The resolved base path for templates.</param>
    /// <returns>A MimeMessage object.</returns>
    private async Task<MimeMessage> CreateEmailMessage(EmailRequest request, string templateBasePath)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpConfig.FromName, _smtpConfig.FromEmail));
        message.To.Add(new MailboxAddress(request.ToName, request.ToEmail));
        message.Subject = request.Subject;
        message.Body = await BuildEmailBody(request.Template, request.TemplateModel, templateBasePath);

        return message;
    }

    /// <summary>
    /// Builds the email body from the specified template.
    /// </summary>
    /// <param name="template">Template identifier.</param>
    /// <param name="templateModel">Template model.</param>
    /// <param name="templateBasePath">The resolved base path for templates.</param>
    /// <returns>A MimeEntity representing the email body.</returns>
    private async Task<MimeEntity> BuildEmailBody(string template, Dictionary<string, string> templateModel, string templateBasePath)
    {
        var htmlEmailBody = await FetchEmailTemplate(template, templateModel, templateBasePath);
        var txtEmailBody = await FetchEmailTemplate(template, templateModel, templateBasePath, false);

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlEmailBody,
            TextBody = txtEmailBody
        };

        return bodyBuilder.ToMessageBody();
    }

    /// <summary>
    /// Loads and replaces placeholders within an email template file.
    /// </summary>
    /// <param name="template">The template identifier.</param>
    /// <param name="templateModel">The placeholder values to inject.</param>
    /// <param name="templateBasePath">The resolved base path for templates.</param>
    /// <param name="isHtml">Determines whether to load the HTML or text version.</param>
    /// <returns>The processed template string.</returns>
    private static async Task<string> FetchEmailTemplate(string template, Dictionary<string, string> templateModel, string templateBasePath, bool isHtml = true)
    {
        var templateFileType = isHtml ? "html" : "txt";
        var templatePath = Path.Combine(templateBasePath, template, $"{template}.{templateFileType}");

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Email template not found: {templatePath}");

        var body = await File.ReadAllTextAsync(templatePath);

        if (templateModel != null)
        {
            foreach (var (key, value) in templateModel)
            {
                body = Regex.Replace(body, $"{{{{{key}}}}}", value, RegexOptions.IgnoreCase);
            }
        }

        return body;
    }

    #endregion
}