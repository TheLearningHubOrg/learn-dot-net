using LearnDotNet.Api.Models;

namespace LearnDotNet.Api.Services;

public interface IRoadmapService
{
    Task<IReadOnlyList<RoadmapStage>> GetAllStagesAsync(CancellationToken cancellationToken);
    Task<RoadmapStage?> GetStageAsync(int stageNumber, CancellationToken cancellationToken);
    Task<RoadmapProgress> GetProgressAsync(CancellationToken cancellationToken);
    Task<RoadmapStage?> SetCompletedAsync(int stageNumber, bool completed, CancellationToken cancellationToken);
    Task<IReadOnlyList<StageNote>?> GetNotesAsync(int stageNumber, CancellationToken cancellationToken);
    Task<NoteResult> AddNoteAsync(int stageNumber, string text, CancellationToken cancellationToken);
    SystemMetrics GetMetrics();
}

// A closed set of outcomes keeps HTTP concerns out of the service: the
// endpoint maps each case to a status code with a switch expression.
public abstract record NoteResult
{
    public sealed record Created(StageNote Note) : NoteResult;
    public sealed record StageNotFound : NoteResult;
    public sealed record LimitReached(int Limit) : NoteResult;
}
