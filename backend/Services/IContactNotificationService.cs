using TraceAI.Api.Models;

namespace TraceAI.Api.Services;

public interface IContactNotificationService
{
    Task SendSupportNotificationAsync(ContactSubmission submission, CancellationToken cancellationToken);
}
