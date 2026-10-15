namespace LearnDotNet.Api.Models;

public record RoadmapStage(
    int StageNumber,
    string Title,
    string DomainFocus,
    bool Completed,
    IReadOnlyList<string> KeyTopics
);

public record RoadmapProgress(
    int TotalStages,
    int CompletedStages,
    double CompletionPercentage,
    IReadOnlyList<RoadmapStage> Stages
);
