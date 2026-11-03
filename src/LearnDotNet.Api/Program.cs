using LearnDotNet.Api.Data;
using LearnDotNet.Api.Endpoints;
using LearnDotNet.Api.Errors;
using LearnDotNet.Api.Middleware;
using LearnDotNet.Api.Options;
using LearnDotNet.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- 1. Services: everything registered here goes into the DI container ----

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidation();
builder.Services.AddHealthChecks();

// Options pattern: bind, validate the data annotations, and fail at startup.
builder.Services.AddOptions<RoadmapOptions>()
    .Bind(builder.Configuration.GetSection(RoadmapOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// The connection string is read when the DbContext options are built, not
// here, so test hosts can override "ConnectionStrings:Roadmap".
builder.Services.AddDbContext<RoadmapDbContext>((sp, options) =>
    options.UseSqlite(sp.GetRequiredService<IConfiguration>().GetConnectionString("Roadmap"))
        .UseSeeding((context, _) => RoadmapSeed.Seed(context))
        .UseAsyncSeeding((context, _, ct) => RoadmapSeed.SeedAsync(context, ct)));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IRoadmapService, RoadmapService>();

var app = builder.Build();

// ---- 2. Startup work that needs a scope (DbContext is scoped) ----

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoadmapDbContext>();
    // A teaching shortcut: production apps apply migrations instead (Stage 3.8).
    await db.Database.EnsureCreatedAsync();
}

// ---- 3. Middleware pipeline: order matters, requests flow top to bottom ----

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // serves /openapi/v1.json
}

// ---- 4. Endpoints ----

app.MapHealthChecks("/health");

app.MapGet("/", (Microsoft.Extensions.Options.IOptions<RoadmapOptions> options) => TypedResults.Ok(new
{
    name = options.Value.Title,
    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    endpoints = new[] { "/health", "/api/v1/roadmap", "/api/v1/roadmap/progress", "/api/v1/roadmap/metrics" },
}));

app.MapRoadmapEndpoints();

app.Run();

// Makes the compiler-generated Program class visible to WebApplicationFactory<Program>.
public partial class Program;
