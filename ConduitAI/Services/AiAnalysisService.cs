using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Services.Ai;
using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Services;

/// <summary>
/// Generates and stores lead intelligence on explicit user request. The latest
/// stored analysis is reused for display; generation never happens on page load.
/// </summary>
public class AiAnalysisService : IAiAnalysisService
{
    private const int RecentInteractionLimit = 12;

    private readonly AppDbContext _db;
    private readonly IOllamaClient _ollama;
    private readonly AiPromptBuilder _promptBuilder;
    private readonly AiResponseParser _parser;
    private readonly ILogger<AiAnalysisService> _logger;

    public AiAnalysisService(
        AppDbContext db,
        IOllamaClient ollama,
        AiPromptBuilder promptBuilder,
        AiResponseParser parser,
        ILogger<AiAnalysisService> logger)
    {
        _db = db;
        _ollama = ollama;
        _promptBuilder = promptBuilder;
        _parser = parser;
        _logger = logger;
    }

    public async Task<AiAnalysisViewModel?> GetLatestAsync(int leadId)
    {
        var latest = await _db.LeadAnalyses
            .AsNoTracking()
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.GeneratedAt)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync();

        return latest is null ? null : AiAnalysisViewModel.FromEntity(latest);
    }

    public async Task<AiOperationResult<AiAnalysisViewModel>> GenerateAsync(int leadId, CancellationToken ct = default)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId, ct);
        if (lead is null)
        {
            return AiOperationResult<AiAnalysisViewModel>.Fail("Lead not found.");
        }

        var recent = await _db.LeadInteractions
            .AsNoTracking()
            .Where(i => i.LeadId == leadId)
            .OrderByDescending(i => i.OccurredAt)
            .ThenByDescending(i => i.Id)
            .Take(RecentInteractionLimit)
            .ToListAsync(ct);

        var prompt = _promptBuilder.BuildLeadAnalysisPrompt(lead, recent);

        var parsed = await GenerateAndParseAsync(prompt, _parser.ParseLeadAnalysis, ct);
        if (!parsed.Success || parsed.Value is null)
        {
            return AiOperationResult<AiAnalysisViewModel>.Fail(
                parsed.ErrorMessage ?? "AI analysis could not be generated.");
        }

        var result = parsed.Value;
        var analysis = new LeadAnalysis
        {
            LeadId = leadId,
            Summary = result.Summary,
            LeadScore = result.LeadScore,
            UrgencyLevel = result.UrgencyLevel,
            BuyingIntent = result.BuyingIntent,
            RecommendedNextAction = result.RecommendedNextAction,
            GeneratedAt = DateTime.UtcNow,
            ModelName = _ollama.ModelName,
            PromptVersion = AiPromptBuilder.LeadAnalysisPromptVersion
        };

        _db.LeadAnalyses.Add(analysis);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Generated analysis {AnalysisId} for lead {LeadId}.", analysis.Id, leadId);
        return AiOperationResult<AiAnalysisViewModel>.Ok(AiAnalysisViewModel.FromEntity(analysis));
    }

    /// <summary>
    /// Calls the model, parses the result, and retries once with a repair prompt
    /// if the first response is unparseable. Partial output is never returned.
    /// </summary>
    private async Task<AiOperationResult<T>> GenerateAndParseAsync<T>(
        string prompt,
        Func<string, AiOperationResult<T>> parse,
        CancellationToken ct)
    {
        var first = await _ollama.GenerateAsync(prompt, ct);
        if (!first.Success)
        {
            return AiOperationResult<T>.Fail(first.ErrorMessage ?? "The local model is not available.");
        }

        var parsed = parse(first.Content);
        if (parsed.Success)
        {
            return parsed;
        }

        _logger.LogWarning("First AI response failed parsing; retrying with repair prompt.");

        var repairPrompt = _promptBuilder.BuildRepairPrompt(prompt, first.Content);
        var second = await _ollama.GenerateAsync(repairPrompt, ct);
        if (!second.Success)
        {
            return AiOperationResult<T>.Fail(second.ErrorMessage ?? "The local model is not available.");
        }

        var reparsed = parse(second.Content);
        if (reparsed.Success)
        {
            return reparsed;
        }

        _logger.LogWarning("AI response failed parsing after repair attempt.");
        return AiOperationResult<T>.Fail(
            "The AI response could not be understood. Please try generating again.");
    }
}
