using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TraceAI.Api.Models;
using TraceAI.Api.Options;

namespace TraceAI.Api.Services.Auth;

public class TokenService(IOptions<AuthOptions> authOptions) : ITokenService
{
    private readonly AuthOptions _auth = authOptions.Value;

    public (string token, DateTime expiresAtUtc) CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_auth.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
            new("phone_verified", user.PhoneVerified.ToString().ToLowerInvariant())
        };

        if (!string.IsNullOrWhiteSpace(user.OAuthProvider))
        {
            claims.Add(new Claim("oauth_provider", user.OAuthProvider));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_auth.JwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_auth.JwtIssuer, _auth.JwtAudience, claims, now, expires, creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string CreateSecureRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashOpaqueToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
