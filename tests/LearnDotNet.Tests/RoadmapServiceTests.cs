using LearnDotNet.Api.Data;
using LearnDotNet.Api.Options;
using LearnDotNet.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnDotNet.Tests;

// Unit tests for the service with no web host. An open in-memory SQLite
// connection keeps the database alive for the lifetime of the test class.
public sealed class RoadmapServiceTests : IDisposable
{
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 3, 9, 30, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly RoadmapDbContext _db;

    public RoadmapServiceTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<RoadmapDbContext>()
            .UseSqlite(_connection)
            .UseSeeding((context, _) => RoadmapSeed.Seed(context))
            .Options;
        _db = new RoadmapDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private RoadmapService CreateService(int maxNotes = 20) =>
        new(_db, new StaticSnapshot(new RoadmapOptions { Title = "Test", MaxNotesPerStage = maxNotes }), new FixedClock(FixedNow));

    [Fact]
    public async Task Progress_percentage_matches_seeded_completion()
    {
        var progress = await CreateService().GetProgressAsync(CancellationToken.None);

        Assert.Equal(7, progress.TotalStages);
        Assert.Equal(3, progress.CompletedStages);
        Assert.Equal(42.86, progress.CompletionPercentage);
    }

    [Fact]
    public async Task Missing_stage_returns_null_instead_of_throwing()
    {
        var service = CreateService();

        Assert.Null(await service.GetStageAsync(8, CancellationToken.None));
        Assert.Null(await service.SetCompletedAsync(8, true, CancellationToken.None));
        Assert.Null(await service.GetNotesAsync(8, CancellationToken.None));
    }

    [Fact]
    public async Task New_note_uses_the_injected_clock()
    {
        var result = await CreateService().AddNoteAsync(1, "Kestrel listens on 8080 in containers", CancellationToken.None);

        var created = Assert.IsType<NoteResult.Created>(result);
        Assert.Equal(FixedNow, created.Note.CreatedAt);
    }

    [Fact]
    public async Task Note_limit_from_options_is_enforced()
    {
        var service = CreateService(maxNotes: 1);

        Assert.IsType<NoteResult.Created>(await service.AddNoteAsync(2, "first note", CancellationToken.None));
        var second = await service.AddNoteAsync(2, "second note", CancellationToken.None);

        Assert.Equal(1, Assert.IsType<NoteResult.LimitReached>(second).Limit);
    }

    [Fact]
    public void Metrics_describe_the_running_runtime()
    {
        var metrics = CreateService().GetMetrics();

        Assert.StartsWith(".NET", metrics.Framework);
        Assert.True(metrics.ProcessorCount > 0);
        Assert.True(metrics.WorkingSetBytes > 0);
        Assert.Equal(FixedNow, metrics.TimestampUtc);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StaticSnapshot(RoadmapOptions value) : IOptionsSnapshot<RoadmapOptions>
    {
        public RoadmapOptions Value => value;
        public RoadmapOptions Get(string? name) => value;
    }
}
