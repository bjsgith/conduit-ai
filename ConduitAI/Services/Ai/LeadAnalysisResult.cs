using ConduitAI.Models.Enums;

namespace ConduitAI.Services.Ai;

/// <summary>
/// Normalized, validated lead-analysis output parsed from the model's JSON.
/// </summary>
public class LeadAnalysisResult
{
    public string Summary { get; set; } = string.Empty;
    public int LeadScore { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public BuyingIntent BuyingIntent { get; set; }
    public string RecommendedNextAction { get; set; } = string.Empty;
}
