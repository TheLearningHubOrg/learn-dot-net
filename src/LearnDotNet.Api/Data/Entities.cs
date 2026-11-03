namespace LearnDotNet.Api.Data;

// Persistence types are kept apart from the API records in Models/ so the
// database schema and the HTTP contract can change independently.
public sealed class StageEntity
{
    public int Id { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public required string DomainFocus { get; set; }
    public bool Completed { get; set; }

    // EF Core stores a primitive collection as a JSON array in one column.
    public List<string> Topics { get; set; } = [];

    public List<NoteEntity> Notes { get; set; } = [];
}

public sealed class NoteEntity
{
    public int Id { get; set; }
    public int StageId { get; set; }
    public StageEntity? Stage { get; set; }
    public required string Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
