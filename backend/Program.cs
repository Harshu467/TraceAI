using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using TraceAI.Api.Data;
using TraceAI.Api.Engine;
using TraceAI.Api.Hubs;
using TraceAI.Api.Middleware;
using TraceAI.Api.Services;
using TraceAI.Api.Steps;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TraceAiDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IAiService, OpenAiService>();
builder.Services.AddScoped<IStepLogService, StepLogService>();
builder.Services.AddScoped<IAgentExecutionEngine, AgentExecutionEngine>();
builder.Services.AddScoped<IContactNotificationService, ContactNotificationService>();

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
app.MapControllers();
app.MapHub<ExecutionHub>("/hubs/execution");
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraceAiDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
