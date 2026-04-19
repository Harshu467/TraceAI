using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TraceAI.Api.Data;
using TraceAI.Api.Engine;
using TraceAI.Api.Hubs;
using TraceAI.Api.Middleware;
using TraceAI.Api.Options;
using TraceAI.Api.Services;
using TraceAI.Api.Services.Auth;
using TraceAI.Api.Services.OAuth;
using TraceAI.Api.Services.Otp;
using TraceAI.Api.Steps;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));
builder.Services.Configure<GoogleOAuthOptions>(builder.Configuration.GetSection(GoogleOAuthOptions.SectionName));

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOptions.JwtIssuer,
            ValidAudience = authOptions.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.JwtSigningKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<TraceAiDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IAiService, OpenAiService>();
builder.Services.AddHttpClient<GoogleOAuthClient>();
builder.Services.AddScoped<IStepLogService, StepLogService>();
builder.Services.AddScoped<IAgentExecutionEngine, AgentExecutionEngine>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IOtpProvider, ConsoleOtpProvider>();

builder.Services.AddScoped<IAgentStep, PlanStep>();
builder.Services.AddScoped<IAgentStep, CodeGenerationStep>();
builder.Services.AddScoped<IAgentStep, ValidationStep>();
builder.Services.AddScoped<IAgentStep, ExplainCodeStep>();
builder.Services.AddScoped<IAgentStep, FixIssuesStep>();
builder.Services.AddScoped<IAgentStep, ImproveStructureStep>();

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["http://localhost:5173"];

    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("frontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ExecutionHub>("/hubs/execution");
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraceAiDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
