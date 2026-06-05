using ConduitAI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConduitAI.Controllers;

/// <summary>
/// AJAX endpoints for explicit AI generation. Analysis is only produced on a
/// user-initiated POST, never on page load.
/// </summary>
public class AiController : Controller
{
    private readonly IAiAnalysisService _aiAnalysis;

    public AiController(IAiAnalysisService aiAnalysis)
    {
        _aiAnalysis = aiAnalysis;
    }

    // POST /Ai/AnalyzeLead/5
    [HttpPost]
    [Route("Ai/AnalyzeLead/{leadId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeLead(int leadId, CancellationToken ct)
    {
        var result = await _aiAnalysis.GenerateAsync(leadId, ct);

        if (!result.Success || result.Value is null)
        {
            return Json(new
            {
                success = false,
                message = result.ErrorMessage ?? "AI analysis could not be generated."
            });
        }

        var a = result.Value;
        return Json(new
        {
            success = true,
            analysisId = a.Id,
            summary = a.Summary,
            leadScore = a.LeadScore,
            urgencyLevel = a.UrgencyLevel.ToString(),
            buyingIntent = a.BuyingIntent.ToString(),
            recommendedNextAction = a.RecommendedNextAction,
            generatedAt = a.GeneratedAt,
            modelName = a.ModelName
        });
    }
}
