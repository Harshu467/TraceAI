using System.Net;
using System.Net.Mail;
using TraceAI.Api.Models;

namespace TraceAI.Api.Services;

public class ContactNotificationService(IConfiguration configuration, ILogger<ContactNotificationService> logger)
    : IContactNotificationService
{
    public async Task SendSupportNotificationAsync(ContactSubmission submission, CancellationToken cancellationToken)
    {
        var smtpHost = configuration["SupportEmail:SmtpHost"];
        var supportEmail = configuration["SupportEmail:To"];
        var fromEmail = configuration["SupportEmail:From"];

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(supportEmail) || string.IsNullOrWhiteSpace(fromEmail))
        {
            logger.LogWarning("Support email settings are missing. Skipping notification for submission {SubmissionId}", submission.Id);
            return;
        }

        using var client = new SmtpClient(smtpHost)
        {
            Port = int.TryParse(configuration["SupportEmail:Port"], out var port) ? port : 25,
            EnableSsl = bool.TryParse(configuration["SupportEmail:EnableSsl"], out var sslEnabled) && sslEnabled
        };

        var username = configuration["SupportEmail:Username"];
        var password = configuration["SupportEmail:Password"];
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        var mail = new MailMessage(fromEmail, supportEmail)
        {
            Subject = $"New Contact Submission from {submission.Name}",
            Body = $"Name: {submission.Name}\nEmail: {submission.Email}\nSubmitted: {submission.SubmittedAtUtc:u}\n\nMessage:\n{submission.Message}"
        };

        using var registration = cancellationToken.Register(() => client.SendAsyncCancel());
        await client.SendMailAsync(mail, cancellationToken);
    }
}
