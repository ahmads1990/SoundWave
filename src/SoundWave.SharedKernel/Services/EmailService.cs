using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
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

    private const string TemplatesFolder = "Templates";
    private const string EmailTemplatesFolder = "EmailTemplates";

    #endregion

    #region Fields

    private readonly SMTPConfig _smtpConfig;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmailService> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="smtpConfig">The SMTP configuration settings.</param>
    /// <param name="env">The web hosting environment for locating template files.</param>
    /// <param name="logger">Logger instance.</param>
    public EmailService(IOptions<SMTPConfig> smtpConfig, IWebHostEnvironment env, ILogger<EmailService> logger)
    {
        _smtpConfig = smtpConfig.Value;
        _env = env;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public async Task SendEmailAsync(EmailRequest request, string projectName, CancellationToken cancellationToken = default)
    {
        var rootPath = GetRootPath(projectName);
        ValidateEmailParameters(request.ToEmail, request.Subject);

        var message = await CreateEmailMessage(request, rootPath);
        using (var client = new SmtpClient())
        {
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
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Resolves the root path for email templates.
    /// </summary>
    /// <param name="projectName">The name of the project to resolve template paths.</param>
    private string GetRootPath(string projectName)
    {
        if (string.IsNullOrEmpty(projectName))
            throw new ArgumentException("Project name must be provided for email template path resolution.", nameof(projectName));
        return Path.Combine(_env.ContentRootPath, TemplatesFolder, projectName, EmailTemplatesFolder);
    }

    /// <summary>
    /// Validates that essential email parameters are provided.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    private void ValidateEmailParameters(string toEmail, string subject)
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
    /// <param name="rootPath">The root path for templates.</param>
    /// <returns>A MimeMessage object.</returns>
    private async Task<MimeMessage> CreateEmailMessage(EmailRequest request, string rootPath)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpConfig.FromName, _smtpConfig.FromEmail));
        message.To.Add(new MailboxAddress(request.ToName, request.ToEmail));
        message.Subject = request.Subject;
        message.Body = await BuildEmailBody(request.Template, request.TemplateModel, rootPath);

        return message;
    }

    /// <summary>
    /// Builds the email body from the specified template.
    /// </summary>
    /// <param name="template">Template identifier.</param>
    /// <param name="templateModel">Template model.</param>
    /// <returns>A MimeEntity representing the email body.</returns>
    private async Task<MimeEntity> BuildEmailBody(string template, Dictionary<string, string> templateModel, string rootPath)
    {
        var htmlEmailBody = await FetchEmailTemplate(template, templateModel, rootPath);
        var txtEmailBody = await FetchEmailTemplate(template, templateModel, rootPath, false);

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
    /// <param name="isHtml">Determines whether to load the HTML or text version.</param>
    /// <returns>The processed template string.</returns>
    private async Task<string> FetchEmailTemplate(string template, Dictionary<string, string> templateModel, string rootPath, bool isHtml = true)
    {
        var templateName = template.ToString();
        var templateFileType = isHtml ? "html" : "txt";
        var templatePath = Path.Combine(rootPath, templateName, $"{templateName}.{templateFileType}");

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