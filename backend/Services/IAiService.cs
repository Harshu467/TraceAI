namespace TraceAI.Api.Services;

public interface IAiService
{
    Task<(string Output, string RawResponse)> CompleteAsync(string prompt, string model, CancellationToken cancellationToken);
}
