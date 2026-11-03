using System.Diagnostics;
using System.Runtime.InteropServices;
using LearnDotNet.Api.Data;
using LearnDotNet.Api.Models;
using LearnDotNet.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnDotNet.Api.Services;

// Registered as scoped: it depends on RoadmapDbContext, which is scoped, so
// one instance lives for exactly one HTTP request.
public sealed class RoadmapService(
    RoadmapDbContext db,
    IOptionsSnapshot<RoadmapOptions> options,
    TimeProvider clock) : IRoadmapService
{
    public async Task<IReadOnlyList<RoadmapStage>> GetAllStagesAsync(CancellationToken cancellationToken) =>
        await db.Stages
            .AsNoTracking()
            .OrderBy(s => s.Number)
            .Select(s => new RoadmapStage(s.Number, s.Title, s.DomainFocus, s.Completed, s.Topics))
            .ToListAsync(cancellationToken);

    public async Task<RoadmapStage?> GetStageAsync(int stageNumber, CancellationToken cancellationToken) =>
        await db.Stages
            .AsNoTracking()
            .Where(s => s.Number == stageNumber)
            .Select(s => new RoadmapStage(s.Number, s.Title, s.DomainFocus, s.Completed, s.Topics))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<RoadmapProgress> GetProgressAsync(CancellationToken cancellationToken)
    {
        var stages = await GetAllStagesAsync(cancellationToken);
        var completed = stages.Count(s => s.Completed);
        var percentage = stages.Count == 0 ? 0 : Math.Round(completed * 100.0 / stages.Count, 2);
        return new RoadmapProgress(stages.Count, completed, percentage, stages);
    }

    public async Task<RoadmapStage?> SetCompletedAsync(int stageNumber, bool completed, CancellationToken cancellationToken)
    {
        // Tracked query: EF Core snapshots the entity, detects the change to
        // Completed and issues a single UPDATE in SaveChangesAsync.
        var stage = await db.Stages.FirstOrDefaultAsync(s => s.Number == stageNumber, cancellationToken);
        if (stage is null)
        {
            return null;
        }

        stage.Completed = completed;
        await db.SaveChangesAsync(cancellationToken);
        return new RoadmapStage(stage.Number, stage.Title, stage.DomainFocus, stage.Completed, stage.Topics);
    }

    public async Task<IReadOnlyList<StageNote>?> GetNotesAsync(int stageNumber, CancellationToken cancellationToken)
    {
        var exists = await db.Stages.AnyAsync(s => s.Number == stageNumber, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await db.Notes
            .AsNoTracking()
            .Where(n => n.Stage!.Number == stageNumber)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new StageNote(n.Id, stageNumber, n.Text, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<NoteResult> AddNoteAsync(int stageNumber, string text, CancellationToken cancellationToken)
    {
        var stage = await db.Stages
            .Where(s => s.Number == stageNumber)
            .Select(s => new { s.Id, NoteCount = s.Notes.Count })
            .FirstOrDefaultAsync(cancellationToken);

        if (stage is null)
        {
            return new NoteResult.StageNotFound();
        }

        var limit = options.Value.MaxNotesPerStage;
        if (stage.NoteCount >= limit)
        {
            return new NoteResult.LimitReached(limit);
        }

        var note = new NoteEntity { StageId = stage.Id, Text = text.Trim(), CreatedAt = clock.GetUtcNow() };
        db.Notes.Add(note);
        await db.SaveChangesAsync(cancellationToken);
        return new NoteResult.Created(new StageNote(note.Id, stageNumber, note.Text, note.CreatedAt));
    }

    public SystemMetrics GetMetrics()
    {
        using var process = Process.GetCurrentProcess();
        return new SystemMetrics(
            Framework: RuntimeInformation.FrameworkDescription,
            RuntimeVersion: Environment.Version.ToString(),
            OsPlatform: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            WorkingSetBytes: process.WorkingSet64,
            TimestampUtc: clock.GetUtcNow());
    }
}
