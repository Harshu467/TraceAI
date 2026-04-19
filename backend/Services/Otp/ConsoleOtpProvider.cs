using Microsoft.Extensions.Logging;

namespace TraceAI.Api.Services.Otp;

public class ConsoleOtpProvider(ILogger<ConsoleOtpProvider> logger) : IOtpProvider
{
    public Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("OTP dispatched to {PhoneNumber}. Code: {Code}", phoneNumber, code);
        return Task.CompletedTask;
    }
}
