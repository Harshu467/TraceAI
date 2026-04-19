namespace TraceAI.Api.Services.Otp;

public interface IOtpProvider
{
    Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}
