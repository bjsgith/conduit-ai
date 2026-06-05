using ConduitAI.Data;
using ConduitAI.Services.Ai;
using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Tests;

/// <summary>
/// Builds isolated in-memory <see cref="AppDbContext"/> instances for tests.
/// </summary>
internal static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"conduitai-tests-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(options);
    }
}

/// <summary>
/// Ollama stub that returns queued canned responses (or a fixed failure),
/// and records how many times it was called.
/// </summary>
internal sealed class FakeOllamaClient : IOllamaClient
{
    private readonly Queue<OllamaResult> _responses = new();

    public FakeOllamaClient(params OllamaResult[] responses)
    {
        foreach (var r in responses)
        {
            _responses.Enqueue(r);
        }
    }

    public int CallCount { get; private set; }
    public string ModelName => "test-model";

    public Task<OllamaResult> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        CallCount++;
        var result = _responses.Count > 0
            ? _responses.Dequeue()
            : OllamaResult.Fail("No more canned responses.");
        return Task.FromResult(result);
    }
}

/// <summary>Minimal AI-analysis stub for LeadService tests.</summary>
internal sealed class StubAiAnalysisService : IAiAnalysisService
{
    public Task<AiAnalysisViewModel?> GetLatestAsync(int leadId) =>
        Task.FromResult<AiAnalysisViewModel?>(null);

    public Task<AiOperationResult<AiAnalysisViewModel>> GenerateAsync(int leadId, CancellationToken ct = default) =>
        Task.FromResult(AiOperationResult<AiAnalysisViewModel>.Fail("not used"));
}

/// <summary>Minimal meeting-notes stub for LeadService tests.</summary>
internal sealed class StubMeetingNotesService : IMeetingNotesService
{
    public Task<AiOperationResult<MeetingNoteSummaryViewModel>> CreateAsync(MeetingNotesFormViewModel form, CancellationToken ct = default) =>
        Task.FromResult(AiOperationResult<MeetingNoteSummaryViewModel>.Fail("not used"));

    public Task<IReadOnlyList<MeetingNoteSummaryViewModel>> GetForLeadAsync(int leadId) =>
        Task.FromResult<IReadOnlyList<MeetingNoteSummaryViewModel>>(new List<MeetingNoteSummaryViewModel>());

    public Task<MeetingNoteSummaryViewModel?> GetByIdAsync(int id) =>
        Task.FromResult<MeetingNoteSummaryViewModel?>(null);
}
