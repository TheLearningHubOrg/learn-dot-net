using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace LearnDotNet.Tests;

// Boots the real Program.cs in memory (no sockets) against a throwaway
// SQLite file, so every test class gets a fresh, seeded database.
public sealed class RoadmapApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"roadmap-{Guid.NewGuid():N}.db");

    public Dictionary<string, string?> Settings { get; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Roadmap", $"Data Source={_databasePath}");
        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // A locked temp file is harmless; the OS cleans the temp folder.
        }
    }
}
