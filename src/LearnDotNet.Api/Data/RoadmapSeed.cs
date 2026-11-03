using Microsoft.EntityFrameworkCore;

namespace LearnDotNet.Api.Data;

// Wired up through UseSeeding/UseAsyncSeeding, which EF Core runs from
// EnsureCreated and from "dotnet ef database update".
public static class RoadmapSeed
{
    public static IReadOnlyList<StageEntity> CreateStages() =>
    [
        Stage(1, ".NET Platform, C# & the ASP.NET Core Host", "SDK, dotnet CLI, project files, C# essentials, WebApplicationBuilder, Kestrel, configuration", true,
            "dotnet CLI", "SDK-style csproj", "WebApplicationBuilder", "Kestrel"),
        Stage(2, "Request Pipeline, Middleware, Routing & Minimal APIs", "Middleware order, routing, minimal API handlers, TypedResults, filters, ProblemDetails", true,
            "RequestDelegate", "MapGroup", "TypedResults", "IExceptionHandler"),
        Stage(3, "Dependency Injection, Options & EF Core Data Access", "Service lifetimes, keyed services, options validation, DbContext, migrations, LINQ", true,
            "Service lifetimes", "IOptions<T>", "DbContext", "Migrations"),
        Stage(4, "Security, Background Work & Real-Time", "JWT bearer, policies, BackgroundService, Channel<T>, SignalR", false,
            "JWT bearer", "Authorization policies", "BackgroundService", "SignalR"),
        Stage(5, "API Design, gRPC & Testing", "OpenAPI, versioning, gRPC, xUnit, WebApplicationFactory", false,
            "OpenAPI", "gRPC", "xUnit", "WebApplicationFactory"),
        Stage(6, "Performance, Resilience, Observability & Deployment", "HybridCache, Polly, OpenTelemetry, health checks, containers, Native AOT", false,
            "HybridCache", "Polly", "OpenTelemetry", "Native AOT"),
        Stage(7, "Staff Interview Handbook", "Interview questions, scenarios and a cheatsheet", false,
            "Internals", "Trade-offs", "Debugging"),
    ];

    public static void Seed(DbContext context)
    {
        var stages = context.Set<StageEntity>();
        if (!stages.Any())
        {
            stages.AddRange(CreateStages());
            context.SaveChanges();
        }
    }

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var stages = context.Set<StageEntity>();
        if (!await stages.AnyAsync(cancellationToken))
        {
            stages.AddRange(CreateStages());
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static StageEntity Stage(int number, string title, string focus, bool completed, params string[] topics) =>
        new() { Number = number, Title = title, DomainFocus = focus, Completed = completed, Topics = [.. topics] };
}
