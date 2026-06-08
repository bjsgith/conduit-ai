using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services;
using ConduitAI.Services.Ai;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConduitAI.Tests;

public class MeetingNotesServiceTests
{
    private const string ValidJson =
        "{\"structuredSummary\":\"Buyer wants single-story home.\",\"keyFacts\":[\"Budget 800k\",\"Scottsdale\"]," +
        "\"risks\":[\"Must sell current home\"],\"recommendedNextAction\":\"Send three listings.\"}";

    private static MeetingNotesService NewService(AppDbContext db, FakeOllamaClient ollama) =>
        new(db, ollama, new AiPromptBuilder(), new AiResponseParser(), NullLogger<MeetingNotesService>.Instance);

    [Fact]
    public async Task CreateAsync_Unattached_StoresNote_NoInteraction()
    {
        using var db = TestDb.Create();
        var svc = NewService(db, new FakeOllamaClient(OllamaResult.Ok(ValidJson)));

        var result = await svc.CreateAsync(new MeetingNotesFormViewModel { RawNotes = "Met the buyer, discussed homes." });

        Assert.True(result.Success);
        Assert.Equal(2, result.Value!.KeyFacts.Count);
        Assert.Single(result.Value.Risks);
        var note = await db.MeetingNotes.SingleAsync();
        Assert.Null(note.LeadId);
        Assert.Equal(AiPromptBuilder.MeetingNotesPromptVersion, note.PromptVersion);
        Assert.Equal(0, await db.LeadInteractions.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_Attached_CreatesInteraction_AndTouchesLead()
    {
        using var db = TestDb.Create();
        var lead = new Lead { Name = "Marcus", LeadSource = LeadSource.Referral, Status = LeadStatus.Qualified, CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow.AddDays(-1) };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        var originalUpdated = lead.UpdatedAt;
        var svc = NewService(db, new FakeOllamaClient(OllamaResult.Ok(ValidJson)));

        var result = await svc.CreateAsync(new MeetingNotesFormViewModel { LeadId = lead.Id, RawNotes = "Met the buyer." });

        Assert.True(result.Success);
        Assert.Equal(lead.Id, result.Value!.LeadId);

        var interaction = await db.LeadInteractions.SingleAsync();
        Assert.Equal(lead.Id, interaction.LeadId);
        Assert.Equal(InteractionType.Meeting, interaction.InteractionType);
        Assert.Equal(DateTimeKind.Utc, interaction.OccurredAt.Kind);
        Assert.Equal(DateTimeKind.Utc, interaction.CreatedAt.Kind);

        Assert.True((await db.Leads.FindAsync(lead.Id))!.UpdatedAt > originalUpdated);
    }

    [Fact]
    public async Task CreateAsync_OllamaUnavailable_StoresNothing()
    {
        using var db = TestDb.Create();
        var svc = NewService(db, new FakeOllamaClient(OllamaResult.Fail("Ollama is not available.")));

        var result = await svc.CreateAsync(new MeetingNotesFormViewModel { RawNotes = "Notes." });

        Assert.False(result.Success);
        Assert.Equal(0, await db.MeetingNotes.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_IncompleteFirstResponse_RetriesOnceThenSucceeds()
    {
        using var db = TestDb.Create();
        var incompleteJson = "{\"structuredSummary\":\"S\",\"recommendedNextAction\":\"A\"}";
        var ollama = new FakeOllamaClient(OllamaResult.Ok(incompleteJson), OllamaResult.Ok(ValidJson));
        var svc = NewService(db, ollama);

        var result = await svc.CreateAsync(new MeetingNotesFormViewModel { RawNotes = "Notes." });

        Assert.True(result.Success);
        Assert.Equal(2, ollama.CallCount);
        Assert.Equal(1, await db.MeetingNotes.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_IncompleteResponses_StoresNothing()
    {
        using var db = TestDb.Create();
        var missingArrays = "{\"structuredSummary\":\"S\",\"recommendedNextAction\":\"A\"}";
        var malformedArrays = "{\"structuredSummary\":\"S\",\"keyFacts\":\"Budget 800k\"," +
                              "\"risks\":[],\"recommendedNextAction\":\"A\"}";
        var ollama = new FakeOllamaClient(OllamaResult.Ok(missingArrays), OllamaResult.Ok(malformedArrays));
        var svc = NewService(db, ollama);

        var result = await svc.CreateAsync(new MeetingNotesFormViewModel { RawNotes = "Notes." });

        Assert.False(result.Success);
        Assert.Equal(2, ollama.CallCount);
        Assert.Equal(0, await db.MeetingNotes.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_OverlongResponses_StoresNothing()
    {
        using var db = TestDb.Create();
        var longSummary = new string('x', AiResponseParser.MaxSummaryLength + 1);
        var overlong = $$"""
        {"structuredSummary":"{{longSummary}}","keyFacts":[],"risks":[],"recommendedNextAction":"A"}
        """;
        var ollama = new FakeOllamaClient(OllamaResult.Ok(overlong), OllamaResult.Ok(overlong));
        var svc = NewService(db, ollama);

        var result = await svc.CreateAsync(new MeetingNotesFormViewModel { RawNotes = "Meeting notes with enough detail." });

        Assert.False(result.Success);
        Assert.Equal(2, ollama.CallCount);
        Assert.Equal(0, await db.MeetingNotes.CountAsync());
        Assert.Equal(0, await db.LeadInteractions.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_UnknownLead_Fails()
    {
        using var db = TestDb.Create();
        var svc = NewService(db, new FakeOllamaClient(OllamaResult.Ok(ValidJson)));

        var result = await svc.CreateAsync(new MeetingNotesFormViewModel { LeadId = 999, RawNotes = "Notes." });

        Assert.False(result.Success);
        Assert.Equal(0, await db.MeetingNotes.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_RoundTripsKeyFactsAndRisks()
    {
        using var db = TestDb.Create();
        var svc = NewService(db, new FakeOllamaClient(OllamaResult.Ok(ValidJson)));

        await svc.CreateAsync(new MeetingNotesFormViewModel { RawNotes = "Notes." });
        var noteId = (await db.MeetingNotes.SingleAsync()).Id;

        var fetched = await svc.GetByIdAsync(noteId);
        Assert.NotNull(fetched);
        Assert.Contains("Budget 800k", fetched!.KeyFacts);
        Assert.Contains("Must sell current home", fetched.Risks);
    }
}
