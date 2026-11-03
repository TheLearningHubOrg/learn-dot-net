using System.ComponentModel.DataAnnotations;

namespace LearnDotNet.Api.Options;

// Bound from the "Roadmap" configuration section. ValidateOnStart() in
// Program.cs makes a bad value stop the host at startup instead of failing
// on the first request that reads it.
public sealed class RoadmapOptions
{
    public const string SectionName = "Roadmap";

    [Required, MinLength(1)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 100)]
    public int MaxNotesPerStage { get; set; } = 20;
}
