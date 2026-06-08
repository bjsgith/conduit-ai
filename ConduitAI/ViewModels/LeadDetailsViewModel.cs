using ConduitAI.Models;

namespace ConduitAI.ViewModels;

/// <summary>
/// Aggregated view for the lead detail page: profile, latest AI analysis,
/// timeline history, attached meeting notes, and an inline interaction form.
/// </summary>
public class LeadDetailsViewModel
{
    public Lead Lead { get; set; } = null!;
    public AiAnalysisViewModel? LatestAnalysis { get; set; }
    public IReadOnlyList<LeadInteraction> Interactions { get; set; } = new List<LeadInteraction>();
    public IReadOnlyList<MeetingNoteSummaryViewModel> MeetingNotes { get; set; } = new List<MeetingNoteSummaryViewModel>();
    public InteractionFormViewModel NewInteraction { get; set; } = new();
}
