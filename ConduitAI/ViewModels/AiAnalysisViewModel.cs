using ConduitAI.Models;
using ConduitAI.Models.Enums;

namespace ConduitAI.ViewModels;

/// <summary>
/// Display-ready view of a stored <see cref="LeadAnalysis"/>.
/// </summary>
public class AiAnalysisViewModel
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int LeadScore { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public BuyingIntent BuyingIntent { get; set; }
    public string RecommendedNextAction { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string ModelName { get; set; } = string.Empty;

    public static AiAnalysisViewModel FromEntity(LeadAnalysis a) => new()
    {
        Id = a.Id,
        LeadId = a.LeadId,
        Summary = a.Summary,
        LeadScore = a.LeadScore,
        UrgencyLevel = a.UrgencyLevel,
        BuyingIntent = a.BuyingIntent,
        RecommendedNextAction = a.RecommendedNextAction,
        GeneratedAt = a.GeneratedAt,
        ModelName = a.ModelName
    };
}
