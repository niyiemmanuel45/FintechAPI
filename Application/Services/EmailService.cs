using System.Text;
using Application.Interfaces;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using FluentEmail.Core;

namespace Application.Services;

public class EmailService : IEmailService
{
    private readonly string _mailGunDomain;
    private readonly string _mailGunApiKey;
    private readonly string _mailGunSenderEmail;
    private readonly string _domain;
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly UserManager<User> _userManager;

    private readonly IFluentEmailFactory _fluentFactory;

    public EmailService(UserManager<User> userManager, IConfiguration configuration, IFluentEmailFactory fluentFactory)
    {
        _mailGunDomain = configuration["Mailgun:Domain"];
        _mailGunApiKey = configuration["Mailgun:ApiKey"];
        _mailGunSenderEmail = configuration["Mailgun:SenderEmail"];
        _apiKey = configuration["SendGrid:ApiKey"];
        _senderEmail = configuration["SendGrid:SenderEmail"];
        _fluentFactory = fluentFactory;
        _userManager = userManager;
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

    public async Task SendEmailAsync(User user, string subject, string htmlContent)
    {
        if (!user.Notifications)
        {
            return;
        }
        var client = new SendGridClient(_apiKey);

        var from = new EmailAddress(_senderEmail, "FintechAPI");

        var to = new EmailAddress(user.Email);

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlContent);

        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to send email. Status Code: {response.StatusCode}");
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
        var client = new SendGridClient(_apiKey);

        var from = new EmailAddress(_senderEmail, "FintechAPI");

        var to = new EmailAddress(user.Email);

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: body);

        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to send email. Status Code: {response.StatusCode}");
        }
    }

    public async Task SendEmailAsync(User user, string subject, string htmlContent, Stream fileContent = null,
        string fileName = null, string contentType = "application/octet-stream")
    {
        var client = new SendGridClient(_apiKey);

        var from = new EmailAddress(_senderEmail, "FintechAPI");

        var to = new EmailAddress(user.Email);

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlContent);

        // Check if a file attachment is provided
        if (fileContent != null && !string.IsNullOrEmpty(fileName))
        {
            using (var memoryStream = new MemoryStream())
            {
                await fileContent.CopyToAsync(memoryStream);
                var base64File = Convert.ToBase64String(memoryStream.ToArray());

                msg.AddAttachment(fileName, base64File, contentType);
            }
        }

        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to send email. Status Code: {response.StatusCode}");
        }
    }
}