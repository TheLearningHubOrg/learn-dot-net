using System.ComponentModel.DataAnnotations;

namespace LearnDotNet.Api.Models;

// Response contracts. Records give value equality and immutability for free,
// and the JSON serializer maps the positional parameters to camelCase fields.
public record RoadmapStage(
    int StageNumber,
    string Title,
    string DomainFocus,
    bool Completed,
    IReadOnlyList<string> KeyTopics);

public record RoadmapProgress(
    int TotalStages,
    int CompletedStages,
    double CompletionPercentage,
    IReadOnlyList<RoadmapStage> Stages);

public record StageNote(int Id, int StageNumber, string Text, DateTimeOffset CreatedAt);

// Request contracts. AddValidation() in Program.cs makes minimal APIs check
// these attributes and answer 400 with a validation problem before the handler runs.
public record CompletionUpdate(bool Completed);

public record NewNote(
    [Required, StringLength(280, MinimumLength = 3)] string Text);
