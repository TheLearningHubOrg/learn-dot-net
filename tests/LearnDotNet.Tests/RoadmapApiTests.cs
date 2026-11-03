using System.Net;
using System.Net.Http.Json;
using LearnDotNet.Api.Middleware;
using LearnDotNet.Api.Models;
using LearnDotNet.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LearnDotNet.Tests;

public sealed class RoadmapApiTests : IDisposable
{
    private readonly RoadmapApiFactory _factory = new();
    private readonly HttpClient _client;

    public RoadmapApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Roadmap_lists_seven_stages_in_order()
    {
        var stages = await _client.GetFromJsonAsync<List<RoadmapStage>>("/api/v1/roadmap");

        Assert.NotNull(stages);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], stages.Select(s => s.StageNumber));
        Assert.All(stages, s => Assert.NotEmpty(s.KeyTopics));
    }

    [Theory]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(7, HttpStatusCode.OK)]
    [InlineData(0, HttpStatusCode.NotFound)]
    [InlineData(99, HttpStatusCode.NotFound)]
    public async Task Single_stage_returns_ok_or_problem_details(int number, HttpStatusCode expected)
    {
        var response = await _client.GetAsync($"/api/v1/roadmap/{number}");

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.NotFound)
        {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Non_numeric_stage_does_not_match_the_int_route_constraint()
    {
        var response = await _client.GetAsync("/api/v1/roadmap/abc");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Completing_a_stage_is_persisted_and_changes_progress()
    {
        var before = await _client.GetFromJsonAsync<RoadmapProgress>("/api/v1/roadmap/progress");

        var put = await _client.PutAsJsonAsync("/api/v1/roadmap/6/completion", new CompletionUpdate(true));
        put.EnsureSuccessStatusCode();

        var after = await _client.GetFromJsonAsync<RoadmapProgress>("/api/v1/roadmap/progress");
        Assert.NotNull(before);
        Assert.NotNull(after);
        Assert.Equal(before.CompletedStages + 1, after.CompletedStages);
        Assert.True(after.Stages.Single(s => s.StageNumber == 6).Completed);
    }

    [Fact]
    public async Task Notes_round_trip_through_the_database()
    {
        var post = await _client.PostAsJsonAsync("/api/v1/roadmap/3/notes", new NewNote("  Scoped beats singleton for DbContext  "));

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var created = await post.Content.ReadFromJsonAsync<StageNote>();
        Assert.NotNull(created);
        Assert.Equal("Scoped beats singleton for DbContext", created.Text);
        Assert.Equal($"/api/v1/roadmap/3/notes/{created.Id}", post.Headers.Location?.OriginalString);

        var notes = await _client.GetFromJsonAsync<List<StageNote>>("/api/v1/roadmap/3/notes");
        Assert.NotNull(notes);
        Assert.Single(notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hi")]
    public async Task Invalid_note_is_rejected_with_a_validation_problem(string text)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/roadmap/3/notes", new NewNote(text));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(problem.Errors.Keys, k => k.Equals("Text", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Note_for_missing_stage_returns_404()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/roadmap/42/notes", new NewNote("valid text"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Correlation_id_is_echoed_when_safe_and_replaced_when_not()
    {
        using var safe = new HttpRequestMessage(HttpMethod.Get, "/health");
        safe.Headers.Add(CorrelationIdMiddleware.HeaderName, "abc-123");
        var safeResponse = await _client.SendAsync(safe);
        Assert.Equal("abc-123", safeResponse.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());

        using var hostile = new HttpRequestMessage(HttpMethod.Get, "/health");
        hostile.Headers.Add(CorrelationIdMiddleware.HeaderName, "bad value; drop table");
        var hostileResponse = await _client.SendAsync(hostile);
        var replaced = hostileResponse.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.Equal(32, replaced.Length);
    }
}

public sealed class ErrorHandlingTests
{
    [Fact]
    public async Task Unhandled_exception_becomes_a_500_problem_without_the_message()
    {
        using var factory = new RoadmapApiFactory();
        using var client = factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(services =>
                services.AddScoped<IRoadmapService, ThrowingRoadmapService>()))
            .CreateClient();

        var response = await client.GetAsync("/api/v1/roadmap");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("traceId", body);
        Assert.DoesNotContain(ThrowingRoadmapService.SecretMessage, body);
    }

    [Fact]
    public void Invalid_options_stop_the_host_at_startup()
    {
        using var factory = new RoadmapApiFactory();
        factory.Settings["Roadmap:MaxNotesPerStage"] = "0";

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(Flatten(error), e => e is OptionsValidationException);
    }

    private static IEnumerable<Exception> Flatten(Exception? e)
    {
        for (; e is not null; e = e.InnerException)
        {
            yield return e;
            if (e is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions.SelectMany(Flatten))
                {
                    yield return inner;
                }
            }
        }
    }

    private sealed class ThrowingRoadmapService : IRoadmapService
    {
        public const string SecretMessage = "connection string password=hunter2";

        public Task<IReadOnlyList<RoadmapStage>> GetAllStagesAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(SecretMessage);

        public Task<RoadmapStage?> GetStageAsync(int stageNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RoadmapProgress> GetProgressAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RoadmapStage?> SetCompletedAsync(int stageNumber, bool completed, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<StageNote>?> GetNotesAsync(int stageNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<NoteResult> AddNoteAsync(int stageNumber, string text, CancellationToken cancellationToken) => throw new NotSupportedException();
        public SystemMetrics GetMetrics() => throw new NotSupportedException();
    }
}
