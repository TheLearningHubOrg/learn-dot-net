using LearnDotNet.Api.Endpoints;
using LearnDotNet.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Dependency Injection Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Learn .NET 8 / ASP.NET Core Enterprise Architecture API",
        Version = "v1",
        Description = "Production-grade reference implementation demonstrating modern Minimal APIs, Kestrel hosting, DI, and Health Checks."
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IRoadmapService, RoadmapService>();

var app = builder.Build();

// 2. HTTP Request Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LearnDotNet API v1");
        c.RoutePrefix = "swagger";
    });
}

// 3. System Endpoints & Routing
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Json(new
{
    name = "The Learning Hub - Modern ASP.NET Core Enterprise Architecture API",
    version = "1.0.0",
    framework = ".NET 8.0 LTS",
    endpoints = new
    {
        health = "/health",
        swagger = "/swagger",
        roadmap = "/api/v1/roadmap",
        progress = "/api/v1/roadmap/progress",
        metrics = "/api/v1/roadmap/metrics"
    }
}));

app.MapRoadmapEndpoints();

Console.WriteLine("======================================================");
Console.WriteLine("  The Learning Hub - ASP.NET Core & .NET Architecture");
Console.WriteLine("======================================================");
Console.WriteLine("Listening on standard Kestrel endpoints.");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
