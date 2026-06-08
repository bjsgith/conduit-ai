using ConduitAI.Models;

namespace ConduitAI.ViewModels;

/// <summary>
/// Display-ready meeting note with parsed key facts and risks.
/// </summary>
public class MeetingNoteSummaryViewModel
{
    public int Id { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public string RawNotes { get; set; } = string.Empty;
    public string StructuredSummary { get; set; } = string.Empty;
    public IReadOnlyList<string> KeyFacts { get; set; } = new List<string>();
    public IReadOnlyList<string> Risks { get; set; } = new List<string>();
    public string RecommendedNextAction { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ModelName { get; set; } = string.Empty;
}
