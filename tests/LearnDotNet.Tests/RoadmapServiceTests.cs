using LearnDotNet.Api.Services;
using Xunit;

namespace LearnDotNet.Tests;

public class RoadmapServiceTests
{
    private readonly RoadmapService _service;

    public RoadmapServiceTests()
    {
        _service = new RoadmapService();
    }

    [Fact]
    public void GetAllStages_ShouldReturnSevenStages()
    {
        // Act
        var stages = _service.GetAllStages();

        // Assert
        Assert.NotNull(stages);
        Assert.Equal(7, stages.Count);
        Assert.Equal(1, stages[0].StageNumber);
        Assert.Equal(7, stages[6].StageNumber);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    [InlineData(0, false)]
    public void GetStageByNumber_ShouldReturnExpectedResult(int stageNumber, bool shouldExist)
    {
        // Act
        var stage = _service.GetStageByNumber(stageNumber);

        // Assert
        if (shouldExist)
        {
            Assert.NotNull(stage);
            Assert.Equal(stageNumber, stage.StageNumber);
            Assert.NotEmpty(stage.Title);
            Assert.NotEmpty(stage.KeyTopics);
        }
        else
        {
            Assert.Null(stage);
        }
    }

    [Fact]
    public void GetProgress_ShouldCalculateAccurateMetrics()
    {
        // Act
        var progress = _service.GetProgress();

        // Assert
        Assert.NotNull(progress);
        Assert.Equal(7, progress.TotalStages);
        Assert.True(progress.CompletedStages >= 5);
        Assert.True(progress.CompletionPercentage > 0 && progress.CompletionPercentage <= 100);
        Assert.Equal(7, progress.Stages.Count);
    }

    [Fact]
    public void GetMetrics_ShouldReturnValidRuntimeTelemetry()
    {
        // Act
        var metrics = _service.GetMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.Contains("ASP.NET Core", metrics.Framework);
        Assert.NotEmpty(metrics.RuntimeVersion);
        Assert.True(metrics.ProcessorCount > 0);
        Assert.True(metrics.WorkingSetBytes > 0);
    }
}
