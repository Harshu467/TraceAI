namespace TraceAI.Api.Contracts.Auth;

public record SignupRequest(string Email, string Password, string? PhoneNumber);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record GoogleCodeExchangeRequest(string Code, string RedirectUri);
public record PhoneOtpRequest(string Email, string PhoneNumber);
public record VerifyPhoneOtpRequest(string Email, string PhoneNumber, string Code);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    UserProfileDto User);

public record UserProfileDto(
    string Email,
    bool EmailVerified,
    bool PhoneVerified,
    string? PhoneNumber,
    string? OAuthProvider);

public record OtpChallengeResponse(
    DateTime ExpiresAtUtc,
    DateTime ResendAvailableAtUtc,
    int MaxAttempts);
