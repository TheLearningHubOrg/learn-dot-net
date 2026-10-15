using LearnDotNet.Api.Models;

namespace LearnDotNet.Api.Services;

public interface IRoadmapService
{
    IReadOnlyList<RoadmapStage> GetAllStages();
    RoadmapStage? GetStageByNumber(int stageNumber);
    RoadmapProgress GetProgress();
    SystemMetrics GetMetrics();
}
