using System.Diagnostics;
using System.Runtime.InteropServices;
using LearnDotNet.Api.Models;

namespace LearnDotNet.Api.Services;

public sealed class RoadmapService : IRoadmapService
{
    private static readonly IReadOnlyList<RoadmapStage> DefaultStages = new List<RoadmapStage>
    {
        new(
            1,
            "The Modern ASP.NET Core Hosting Pipeline & Kestrel Architecture",
            "Kestrel HTTP/3 QUIC, WebApplicationBuilder, sockets transport, epoll/IOCP event loops",
            true,
            new[] { "WebApplicationBuilder", "Kestrel Architecture", "HTTP/3 & QUIC", "Sockets Transport" }
        ),
        new(
            2,
            "Middleware Pipeline Architecture, Request Lifecycle & Error Handling",
            "Functional RequestDelegate middleware, ProblemDetails RFC 7807, global exception handlers",
            true,
            new[] { "RequestDelegate", "UseExceptionHandler", "ProblemDetails RFC 7807", "CancellationToken" }
        ),
        new(
            3,
            "Dependency Injection Container Internals, Lifetimes & Keyed Services",
            "Transient/Scoped/Singleton, Keyed Services in .NET 8, captive dependency validation",
            true,
            new[] { "Keyed Services", "Captive Dependencies", "Scoped Validation", "Service Lifetimes" }
        ),
        new(
            4,
            "High-Performance Minimal APIs, Route Handlers & Endpoint Filters",
            "Zero-alloc routing, IEndpointFilter, typed results (TypedResults.Ok), source-generated JSON",
            true,
            new[] { "Route Handlers", "IEndpointFilter", "TypedResults", "Source Generated JSON" }
        ),
        new(
            5,
            "Enterprise Data Access with Entity Framework Core 8/9",
            "DbContext pooling, compiled queries, change tracking interceptors, split queries",
            true,
            new[] { "DbContext Pooling", "Compiled Queries", "Split Queries", "Interceptors" }
        ),
        new(
            6,
            "Identity, JWT Authentication & Policy-Based Authorization",
            "Bearer token validation, policy requirements, claims transformation, role checks",
            false,
            new[] { "JWT Bearer", "AuthorizationHandler", "ClaimsTransformation", "Custom Policies" }
        ),
        new(
            7,
            "Real-Time Systems with SignalR & Background Hosted Services",
            "SignalR hubs, MessagePack binary protocol, IHostedService background workers",
            true,
            new[] { "Hub<T>", "MessagePack Protocol", "BackgroundService", "Channel<T> Consumer" }
        )
    };

    public IReadOnlyList<RoadmapStage> GetAllStages() => DefaultStages;

    public RoadmapStage? GetStageByNumber(int stageNumber) =>
        DefaultStages.FirstOrDefault(s => s.StageNumber == stageNumber);

    public RoadmapProgress GetProgress()
    {
        var completed = DefaultStages.Count(s => s.Completed);
        var percentage = Math.Round((double)completed / DefaultStages.Count * 100, 2);
        return new RoadmapProgress(DefaultStages.Count, completed, percentage, DefaultStages);
    }

    public SystemMetrics GetMetrics()
    {
        var process = Process.GetCurrentProcess();
        return new SystemMetrics(
            Framework: "ASP.NET Core 8.0 LTS",
            RuntimeVersion: Environment.Version.ToString(),
            OsPlatform: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            WorkingSetBytes: process.WorkingSet64,
            TimestampUtc: DateTime.UtcNow
        );
    }
}
