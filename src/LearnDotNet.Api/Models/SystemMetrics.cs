namespace LearnDotNet.Api.Models;

public record SystemMetrics(
    string Framework,
    string RuntimeVersion,
    string OsPlatform,
    int ProcessorCount,
    long WorkingSetBytes,
    DateTime TimestampUtc
);
