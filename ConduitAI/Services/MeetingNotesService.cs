using System.Text.Json;
using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services.Ai;
using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Services;

/// <summary>
/// Converts raw meeting notes into structured business output via the local
/// model, stores both, and (when attached to a lead) logs a timeline entry.
/// </summary>
public class MeetingNotesService : IMeetingNotesService
{
    private readonly AppDbContext _db;
    private readonly IOllamaClient _ollama;
    private readonly AiPromptBuilder _promptBuilder;
    private readonly AiResponseParser _parser;
    private readonly ILogger<MeetingNotesService> _logger;

    public MeetingNotesService(
        AppDbContext db,
        IOllamaClient ollama,
        AiPromptBuilder promptBuilder,
        AiResponseParser parser,
        ILogger<MeetingNotesService> logger)
    {
        _db = db;
        _ollama = ollama;
        _promptBuilder = promptBuilder;
        _parser = parser;
        _logger = logger;
    }

    public async Task<AiOperationResult<MeetingNoteSummaryViewModel>> CreateAsync(
        MeetingNotesFormViewModel form, CancellationToken ct = default)
    {
        Lead? lead = null;
        if (form.LeadId.HasValue)
        {
            lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == form.LeadId.Value, ct);
            if (lead is null)
            {
                return AiOperationResult<MeetingNoteSummaryViewModel>.Fail("The selected lead was not found.");
            }
        }

        var prompt = _promptBuilder.BuildMeetingNotesPrompt(form.RawNotes, lead);

        var parsed = await GenerateAndParseAsync(prompt, ct);
        if (!parsed.Success || parsed.Value is null)
        {
            return AiOperationResult<MeetingNoteSummaryViewModel>.Fail(
                parsed.ErrorMessage ?? "Meeting notes could not be analyzed.");
        }

        var result = parsed.Value;
        var now = DateTime.UtcNow;

        var note = new MeetingNote
        {
            LeadId = form.LeadId,
            RawNotes = form.RawNotes.Trim(),
            StructuredSummary = result.StructuredSummary,
            KeyFactsJson = JsonSerializer.Serialize(result.KeyFacts),
            RisksJson = JsonSerializer.Serialize(result.Risks),
            RecommendedNextAction = result.RecommendedNextAction,
            CreatedAt = now,
            ModelName = _ollama.ModelName,
            PromptVersion = AiPromptBuilder.MeetingNotesPromptVersion
        };

        _db.MeetingNotes.Add(note);

        // When attached to a lead, log a timeline entry and refresh activity.
        if (lead is not null)
        {
            _db.LeadInteractions.Add(new LeadInteraction
            {
                LeadId = lead.Id,
                InteractionType = InteractionType.Meeting,
                OccurredAt = now,
                CreatedAt = now,
                Notes = $"Meeting notes captured. Summary: {result.StructuredSummary}"
            });
            lead.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Stored meeting note {NoteId} (lead {LeadId}).", note.Id, form.LeadId);
        return AiOperationResult<MeetingNoteSummaryViewModel>.Ok(ToSummary(note, lead?.Name));
    }

    public async Task<IReadOnlyList<MeetingNoteSummaryViewModel>> GetForLeadAsync(int leadId)
    {
        var notes = await _db.MeetingNotes
            .AsNoTracking()
            .Where(m => m.LeadId == leadId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return notes.Select(n => ToSummary(n, null)).ToList();
    }

    public async Task<MeetingNoteSummaryViewModel?> GetByIdAsync(int id)
    {
        var note = await _db.MeetingNotes
            .AsNoTracking()
            .Include(m => m.Lead)
            .FirstOrDefaultAsync(m => m.Id == id);

        return note is null ? null : ToSummary(note, note.Lead?.Name);
    }

    private async Task<AiOperationResult<MeetingNotesResult>> GenerateAndParseAsync(string prompt, CancellationToken ct)
    {
        var first = await _ollama.GenerateAsync(prompt, ct);
        if (!first.Success)
        {
            return AiOperationResult<MeetingNotesResult>.Fail(first.ErrorMessage ?? "The local model is not available.");
        }

        var parsed = _parser.ParseMeetingNotes(first.Content);
        if (parsed.Success)
        {
            return parsed;
        }

        _logger.LogWarning("First meeting-notes response failed parsing; retrying with repair prompt.");

        var repairPrompt = _promptBuilder.BuildRepairPrompt(prompt, first.Content);
        var second = await _ollama.GenerateAsync(repairPrompt, ct);
        if (!second.Success)
        {
            return AiOperationResult<MeetingNotesResult>.Fail(second.ErrorMessage ?? "The local model is not available.");
        }

        var reparsed = _parser.ParseMeetingNotes(second.Content);
        if (reparsed.Success)
        {
            return reparsed;
        }

        _logger.LogWarning("Meeting-notes response failed parsing after repair attempt.");
        return AiOperationResult<MeetingNotesResult>.Fail(
            "The AI response could not be understood. Please try again.");
    }

    private static MeetingNoteSummaryViewModel ToSummary(MeetingNote note, string? leadName) => new()
    {
        Id = note.Id,
        LeadId = note.LeadId,
        LeadName = leadName,
        RawNotes = note.RawNotes,
        StructuredSummary = note.StructuredSummary,
        KeyFacts = Deserialize(note.KeyFactsJson),
        Risks = Deserialize(note.RisksJson),
        RecommendedNextAction = note.RecommendedNextAction,
        CreatedAt = note.CreatedAt,
        ModelName = note.ModelName
    };

    private static List<string> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
