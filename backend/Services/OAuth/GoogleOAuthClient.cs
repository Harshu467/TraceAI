using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TraceAI.Api.Options;

namespace TraceAI.Api.Services.OAuth;

public record GoogleUserInfo(string Sub, string Email, bool EmailVerified);

public class GoogleOAuthClient(HttpClient httpClient, IOptions<GoogleOAuthOptions> options)
{
    private readonly GoogleOAuthOptions _options = options.Value;

    public async Task<GoogleUserInfo> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        }), cancellationToken);

        tokenResponse.EnsureSuccessStatusCode();
        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken) ?? throw new InvalidOperationException("Google token payload missing");

        if (!tokenPayload.TryGetValue("access_token", out var accessTokenObj) || accessTokenObj is null)
        {
            throw new InvalidOperationException("Google access token missing");
        }

        var accessToken = accessTokenObj.ToString()!;
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var userInfoResponse = await httpClient.SendAsync(request, cancellationToken);
        userInfoResponse.EnsureSuccessStatusCode();
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken);

        return userInfo ?? throw new InvalidOperationException("Google user info missing");
    }
}
