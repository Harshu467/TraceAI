using Microsoft.EntityFrameworkCore;
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

builder.Services.AddScoped<IAgentStep, PlanStep>();
builder.Services.AddScoped<IAgentStep, CodeGenerationStep>();
builder.Services.AddScoped<IAgentStep, ValidationStep>();

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<ExecutionHub>("/hubs/execution");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraceAiDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
