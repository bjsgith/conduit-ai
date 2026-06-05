using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services;
using ConduitAI.Services.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConduitAI.Tests;

public class AiAnalysisServiceTests
{
    private const string ValidJson =
        "{\"summary\":\"Strong relocating buyer.\",\"leadScore\":84,\"urgencyLevel\":\"High\"," +
        "\"buyingIntent\":\"High\",\"recommendedNextAction\":\"Schedule a tour.\"}";

    private static AiAnalysisService NewService(AppDbContext db, FakeOllamaClient ollama) =>
        new(db, ollama, new AiPromptBuilder(), new AiResponseParser(), NullLogger<AiAnalysisService>.Instance);

    private static async Task<int> SeedLeadAsync(AppDbContext db)
    {
        var lead = new Lead { Name = "Marcus", LeadSource = LeadSource.Referral, Status = LeadStatus.Qualified, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        return lead.Id;
    }

    [Fact]
    public async Task GenerateAsync_ValidResponse_StoresAnalysis()
    {
        using var db = TestDb.Create();
        var leadId = await SeedLeadAsync(db);
        var ollama = new FakeOllamaClient(OllamaResult.Ok(ValidJson));
        var svc = NewService(db, ollama);

        var result = await svc.GenerateAsync(leadId);

        Assert.True(result.Success);
        Assert.Equal(84, result.Value!.LeadScore);
        Assert.Equal("test-model", result.Value.ModelName);
        var stored = await db.LeadAnalyses.SingleAsync();
        Assert.Equal(leadId, stored.LeadId);
        Assert.Equal(UrgencyLevel.High, stored.UrgencyLevel);
        Assert.Equal(AiPromptBuilder.LeadAnalysisPromptVersion, stored.PromptVersion);
    }

    [Fact]
    public async Task GenerateAsync_OllamaUnavailable_FailsAndStoresNothing()
    {
        using var db = TestDb.Create();
        var leadId = await SeedLeadAsync(db);
        var ollama = new FakeOllamaClient(OllamaResult.Fail("Ollama is not available."));
        var svc = NewService(db, ollama);

        var result = await svc.GenerateAsync(leadId);

        Assert.False(result.Success);
        Assert.Equal("Ollama is not available.", result.ErrorMessage);
        Assert.Equal(0, await db.LeadAnalyses.CountAsync());
        Assert.Equal(1, ollama.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_FirstUnparseable_RetriesOnceThenSucceeds()
    {
        using var db = TestDb.Create();
        var leadId = await SeedLeadAsync(db);
        var ollama = new FakeOllamaClient(OllamaResult.Ok("sorry, not json"), OllamaResult.Ok(ValidJson));
        var svc = NewService(db, ollama);

        var result = await svc.GenerateAsync(leadId);

        Assert.True(result.Success);
        Assert.Equal(2, ollama.CallCount);
        Assert.Equal(1, await db.LeadAnalyses.CountAsync());
    }

    [Fact]
    public async Task GenerateAsync_IncompleteFirstResponse_RetriesOnceThenSucceeds()
    {
        using var db = TestDb.Create();
        var leadId = await SeedLeadAsync(db);
        var incompleteJson = "{\"summary\":\"S\",\"urgencyLevel\":\"High\"," +
                             "\"buyingIntent\":\"High\",\"recommendedNextAction\":\"A\"}";
        var ollama = new FakeOllamaClient(OllamaResult.Ok(incompleteJson), OllamaResult.Ok(ValidJson));
        var svc = NewService(db, ollama);

        var result = await svc.GenerateAsync(leadId);

        Assert.True(result.Success);
        Assert.Equal(2, ollama.CallCount);
        Assert.Equal(1, await db.LeadAnalyses.CountAsync());
    }

    [Fact]
    public async Task GenerateAsync_BothAttemptsUnparseable_FailsAndStoresNothing()
    {
        using var db = TestDb.Create();
        var leadId = await SeedLeadAsync(db);
        var ollama = new FakeOllamaClient(OllamaResult.Ok("nope"), OllamaResult.Ok("still nope"));
        var svc = NewService(db, ollama);

        var result = await svc.GenerateAsync(leadId);

        Assert.False(result.Success);
        Assert.Equal(2, ollama.CallCount);
        Assert.Equal(0, await db.LeadAnalyses.CountAsync());
    }

    [Fact]
    public async Task GenerateAsync_IncompleteResponses_FailsAndStoresNothing()
    {
        using var db = TestDb.Create();
        var leadId = await SeedLeadAsync(db);
        var missingScore = "{\"summary\":\"S\",\"urgencyLevel\":\"High\"," +
                           "\"buyingIntent\":\"High\",\"recommendedNextAction\":\"A\"}";
        var invalidIntent = "{\"summary\":\"S\",\"leadScore\":84,\"urgencyLevel\":\"High\"," +
                            "\"buyingIntent\":\"maybe\",\"recommendedNextAction\":\"A\"}";
        var ollama = new FakeOllamaClient(OllamaResult.Ok(missingScore), OllamaResult.Ok(invalidIntent));
        var svc = NewService(db, ollama);

        var result = await svc.GenerateAsync(leadId);

        Assert.False(result.Success);
        Assert.Equal(2, ollama.CallCount);
        Assert.Equal(0, await db.LeadAnalyses.CountAsync());
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsMostRecent_WithoutCallingModel()
    {
        using var db = TestDb.Create();
        var leadId = await SeedLeadAsync(db);
        db.LeadAnalyses.AddRange(
            new LeadAnalysis { LeadId = leadId, Summary = "old", LeadScore = 50, RecommendedNextAction = "x", GeneratedAt = DateTime.UtcNow.AddDays(-2), ModelName = "m", PromptVersion = "v" },
            new LeadAnalysis { LeadId = leadId, Summary = "new", LeadScore = 90, RecommendedNextAction = "y", GeneratedAt = DateTime.UtcNow, ModelName = "m", PromptVersion = "v" });
        await db.SaveChangesAsync();
        var ollama = new FakeOllamaClient();
        var svc = NewService(db, ollama);

        var latest = await svc.GetLatestAsync(leadId);

        Assert.NotNull(latest);
        Assert.Equal(90, latest!.LeadScore);
        Assert.Equal(0, ollama.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_UnknownLead_Fails()
    {
        using var db = TestDb.Create();
        var svc = NewService(db, new FakeOllamaClient(OllamaResult.Ok(ValidJson)));

        var result = await svc.GenerateAsync(12345);

        Assert.False(result.Success);
    }
}
