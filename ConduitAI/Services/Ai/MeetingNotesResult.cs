namespace ConduitAI.Services.Ai;

/// <summary>
/// Normalized, validated meeting-notes output parsed from the model's JSON.
/// </summary>
public class MeetingNotesResult
{
    public string StructuredSummary { get; set; } = string.Empty;
    public List<string> KeyFacts { get; set; } = new();
    public List<string> Risks { get; set; } = new();
    public string RecommendedNextAction { get; set; } = string.Empty;
}
