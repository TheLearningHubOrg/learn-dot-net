using Microsoft.EntityFrameworkCore;

namespace LearnDotNet.Api.Data;

public sealed class RoadmapDbContext(DbContextOptions<RoadmapDbContext> options) : DbContext(options)
{
    public DbSet<StageEntity> Stages => Set<StageEntity>();
    public DbSet<NoteEntity> Notes => Set<NoteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StageEntity>(stage =>
        {
            stage.HasIndex(s => s.Number).IsUnique();
            stage.Property(s => s.Title).HasMaxLength(200);
            stage.Property(s => s.DomainFocus).HasMaxLength(500);
            stage.HasMany(s => s.Notes)
                .WithOne(n => n.Stage)
                .HasForeignKey(n => n.StageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NoteEntity>(note =>
        {
            note.Property(n => n.Text).HasMaxLength(280);
            // SQLite cannot order by DateTimeOffset natively; store it as
            // ticks so ORDER BY CreatedAt translates to SQL.
            note.Property(n => n.CreatedAt).HasConversion(
                v => v.UtcTicks,
                v => new DateTimeOffset(v, TimeSpan.Zero));
        });
    }
}
