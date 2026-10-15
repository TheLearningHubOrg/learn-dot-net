using LearnDotNet.Api.Models;
using LearnDotNet.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LearnDotNet.Api.Endpoints;

public static class RoadmapEndpoints
{
    public static RouteGroupBuilder MapRoadmapEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/roadmap")
            .WithTags("Roadmap")
            .WithOpenApi();

        group.MapGet("/", Ok<IReadOnlyList<RoadmapStage>> (IRoadmapService service) =>
        {
            return TypedResults.Ok(service.GetAllStages());
        })
        .WithName("GetAllStages")
        .WithSummary("Retrieve all 7 enterprise ASP.NET Core roadmap stages");

        group.MapGet("/{stageNumber:int}", Results<Ok<RoadmapStage>, NotFound<string>> (int stageNumber, IRoadmapService service) =>
        {
            var stage = service.GetStageByNumber(stageNumber);
            if (stage is null)
            {
                return TypedResults.NotFound($"Stage {stageNumber} not found in roadmap.");
            }
            return TypedResults.Ok(stage);
        })
        .WithName("GetStageByNumber")
        .WithSummary("Retrieve a specific stage by its number");

        group.MapGet("/progress", Ok<RoadmapProgress> (IRoadmapService service) =>
        {
            return TypedResults.Ok(service.GetProgress());
        })
        .WithName("GetRoadmapProgress")
        .WithSummary("Retrieve overall roadmap completion progress");

        group.MapGet("/metrics", Ok<SystemMetrics> (IRoadmapService service) =>
        {
            return TypedResults.Ok(service.GetMetrics());
        })
        .WithName("GetSystemMetrics")
        .WithSummary("Retrieve runtime and host metrics");

        return group;
    }
}
