using TraceAI.Api.Models;

namespace TraceAI.Api.Services.Auth;

public interface ITokenService
{
    (string token, DateTime expiresAtUtc) CreateAccessToken(User user);
    string CreateSecureRefreshToken();
    string HashOpaqueToken(string token);
}
