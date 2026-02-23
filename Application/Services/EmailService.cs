using System.Text;
using System.Net;
using System.Net.Mail;
using Application.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using FluentEmail.Core;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class EmailService : IEmailService
{
    private readonly string _mailGunDomain;
    private readonly string _mailGunApiKey;
    private readonly string _mailGunSenderEmail;
    private readonly string? _domain;
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly UserManager<User> _userManager;
    private readonly IFluentEmailFactory _fluentFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    // Brevo (Sendinblue) SMTP settings
    private readonly string? _brevoSmtpServer;
    private readonly int _brevoSmtpPort;
    private readonly string? _brevoSmtpLogin;
    private readonly string? _brevoSmtpPassword;
    private readonly string? _brevoSenderEmail;
    private readonly string? _brevoSenderName;
    
    // Brevo API v3 settings
    private readonly string? _brevoApiKey;
    private readonly bool _useBrevoApi;
    private readonly HttpClient _httpClient;

    public EmailService(
        UserManager<User> userManager, 
        IConfiguration configuration, 
        IFluentEmailFactory fluentFactory,
        ILogger<EmailService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _mailGunDomain = configuration["Mailgun:Domain"];
        _mailGunApiKey = configuration["Mailgun:ApiKey"];
        _mailGunSenderEmail = configuration["Mailgun:SenderEmail"];
        _apiKey = configuration["SendGrid:ApiKey"];
        _senderEmail = configuration["SendGrid:SenderEmail"];
        _fluentFactory = fluentFactory;
        _userManager = userManager;

        // Brevo configuration
        _brevoSmtpServer = configuration["Brevo:SmtpServer"] ?? "smtp-relay.brevo.com";
        _brevoSmtpPort = int.Parse(configuration["Brevo:SmtpPort"] ?? "587");
        _brevoSmtpLogin = configuration["Brevo:SmtpLogin"];
        _brevoSmtpPassword = configuration["Brevo:SmtpPassword"];
        _brevoSenderEmail = configuration["Brevo:SenderEmail"];
        _brevoSenderName = configuration["Brevo:SenderName"] ?? "FintechAPI";
        
        // Brevo API v3
        _brevoApiKey = configuration["Brevo:ApiKey"];
        _useBrevoApi = bool.Parse(configuration["Brevo:UseApi"] ?? "true"); // Default to API
        _httpClient = httpClientFactory.CreateClient("BrevoApi");
    }

    public async Task SendEmailMailgunAsync(User user, string subject, string body)
    {
        if (!user.Notifications)
        {
            return;
        }

        var email = _fluentFactory.Create()
            .To(user.Email)
            .Subject(subject)
            .Body(body, isHtml: true);

        var result = await email.SendAsync();
        if (!result.Successful)
        {
            Console.WriteLine($"FluentEmail failed: {string.Join(';', result.ErrorMessages ?? new string[0])}");
        }
    }

    /// <summary>
    /// Send email using Brevo (Sendinblue) SMTP
    /// </summary>
    private async Task SendEmailViaBrevoSmtpAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            using var smtpClient = new SmtpClient(_brevoSmtpServer, _brevoSmtpPort)
            {
                Credentials = new NetworkCredential(_brevoSmtpLogin, _brevoSmtpPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_brevoSenderEmail, _brevoSenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(toEmail, toName));

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email} via Brevo SMTP", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via Brevo SMTP", toEmail);
            throw;
        }
    }

    /// <summary>
    /// Send email using Brevo API v3 (Transactional Email)
    /// </summary>
    private async Task SendEmailViaBrevoApiAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var payload = new
            {
                sender = new
                {
                    name = _brevoSenderName,
                    email = _brevoSenderEmail
                },
                to = new[]
                {
                    new
                    {
                        email = toEmail,
                        name = toName
                    }
                },
                subject = subject,
                htmlContent = htmlBody
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Add("api-key", _brevoApiKey);

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Email sent successfully to {Email} via Brevo API. Response: {Response}", 
                    toEmail, responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {Email} via Brevo API. Status: {Status}, Error: {Error}", 
                    toEmail, response.StatusCode, errorContent);
                throw new Exception($"Brevo API error: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via Brevo API", toEmail);
            throw;
        }
    }

    /// <summary>
    /// Send email using Brevo (automatically chooses API or SMTP based on configuration)
    /// </summary>
    private async Task SendEmailViaBrevoAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (_useBrevoApi && !string.IsNullOrEmpty(_brevoApiKey))
        {
            await SendEmailViaBrevoApiAsync(toEmail, toName, subject, htmlBody);
        }
        else
        {
            await SendEmailViaBrevoSmtpAsync(toEmail, toName, subject, htmlBody);
        }
    }

    public async Task SendEmailAsync(User user, string subject, string htmlContent)
    {
        if (!user.Notifications)
        {
            return;
        }

        try
        {
            // Use Brevo SMTP
            var userName = $"{user.FirstName} {user.LastName}";
            await SendEmailViaBrevoAsync(user.Email, userName, subject, htmlContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", user.Email);
            throw;
        }
    }

    /// <summary>
    /// This method send emails, even if user disabled notifications.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="subject"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public async Task ForceSendEmailAsync(User user, string subject, string body)
    {
        try
        {
            // Use Brevo SMTP - force send regardless of notification preferences
            var userName = $"{user.FirstName} {user.LastName}";
            await SendEmailViaBrevoAsync(user.Email, userName, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force send email to {Email}", user.Email);
            throw;
        }
    }

    public async Task SendEmailAsync(User user, string subject, string htmlContent, Stream fileContent = null,
        string fileName = null, string contentType = "application/octet-stream")
    {
        try
        {
            using var smtpClient = new SmtpClient(_brevoSmtpServer, _brevoSmtpPort)
            {
                Credentials = new NetworkCredential(_brevoSmtpLogin, _brevoSmtpPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var userName = $"{user.FirstName} {user.LastName}";
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_brevoSenderEmail, _brevoSenderName),
                Subject = subject,
                Body = htmlContent,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(user.Email, userName));

            // Check if a file attachment is provided
            if (fileContent != null && !string.IsNullOrEmpty(fileName))
            {
                using var memoryStream = new MemoryStream();
                await fileContent.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                
                var attachment = new System.Net.Mail.Attachment(memoryStream, fileName, contentType);
                mailMessage.Attachments.Add(attachment);
            }

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email with attachment sent successfully to {Email} via Brevo", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment to {Email} via Brevo", user.Email);
            throw;
        }
    }
}