namespace TraceAI.Api.Options;

public class AuthOptions
{
    public const string SectionName = "Auth";
    public string JwtIssuer { get; set; } = "traceai-api";
    public string JwtAudience { get; set; } = "traceai-frontend";
    public string JwtSigningKey { get; set; } = "replace-this-key-with-32-plus-bytes";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
