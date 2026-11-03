using LearnDotNet.Api.Models;
using LearnDotNet.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LearnDotNet.Api.Endpoints;

public static class RoadmapEndpoints
{
    public static RouteGroupBuilder MapRoadmapEndpoints(this IEndpointRouteBuilder routes)
    {
        // Every route below inherits the prefix and the OpenAPI tag.
        var group = routes.MapGroup("/api/v1/roadmap").WithTags("Roadmap");

        group.MapGet("/", GetAll).WithName("GetAllStages");
        group.MapGet("/{stageNumber:int}", GetOne).WithName("GetStageByNumber");
        group.MapPut("/{stageNumber:int}/completion", SetCompletion).WithName("SetStageCompletion");
        group.MapGet("/{stageNumber:int}/notes", GetNotes).WithName("GetStageNotes");
        group.MapPost("/{stageNumber:int}/notes", AddNote).WithName("AddStageNote");
        group.MapGet("/progress", GetProgress).WithName("GetRoadmapProgress");
        group.MapGet("/metrics", (IRoadmapService service) => TypedResults.Ok(service.GetMetrics()))
            .WithName("GetSystemMetrics");

        return group;
    }

    // Handlers are static methods so tests can call them directly and the
    // Results<...> union documents every status code in the OpenAPI output.
    internal static async Task<Ok<IReadOnlyList<RoadmapStage>>> GetAll(IRoadmapService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetAllStagesAsync(ct));

    internal static async Task<Results<Ok<RoadmapStage>, ProblemHttpResult>> GetOne(
        int stageNumber, IRoadmapService service, CancellationToken ct)
    {
        var stage = await service.GetStageAsync(stageNumber, ct);
        return stage is null ? StageNotFound(stageNumber) : TypedResults.Ok(stage);
    }

    internal static async Task<Ok<RoadmapProgress>> GetProgress(IRoadmapService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetProgressAsync(ct));

    internal static async Task<Results<Ok<RoadmapStage>, ProblemHttpResult>> SetCompletion(
        int stageNumber, CompletionUpdate update, IRoadmapService service, CancellationToken ct)
    {
        var stage = await service.SetCompletedAsync(stageNumber, update.Completed, ct);
        return stage is null ? StageNotFound(stageNumber) : TypedResults.Ok(stage);
    }

    internal static async Task<Results<Ok<IReadOnlyList<StageNote>>, ProblemHttpResult>> GetNotes(
        int stageNumber, IRoadmapService service, CancellationToken ct)
    {
        var notes = await service.GetNotesAsync(stageNumber, ct);
        return notes is null ? StageNotFound(stageNumber) : TypedResults.Ok(notes);
    }

    internal static async Task<Results<Created<StageNote>, ProblemHttpResult>> AddNote(
        int stageNumber, NewNote note, IRoadmapService service, CancellationToken ct)
    {
        var result = await service.AddNoteAsync(stageNumber, note.Text, ct);
        return result switch
        {
            NoteResult.Created created =>
                TypedResults.Created($"/api/v1/roadmap/{stageNumber}/notes/{created.Note.Id}", created.Note),
            NoteResult.LimitReached limit => TypedResults.Problem(
                title: "Note limit reached",
                detail: $"Stage {stageNumber} already has {limit.Limit} notes.",
                statusCode: StatusCodes.Status409Conflict),
            _ => StageNotFound(stageNumber),
        };
    }

    private static ProblemHttpResult StageNotFound(int stageNumber) =>
        TypedResults.Problem(
            title: "Stage not found",
            detail: $"Stage {stageNumber} does not exist in the roadmap.",
            statusCode: StatusCodes.Status404NotFound);
}
