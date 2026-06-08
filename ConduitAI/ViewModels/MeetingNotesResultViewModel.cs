namespace ConduitAI.ViewModels;

/// <summary>
/// Standalone result page for a meeting note that was not attached to a lead.
/// </summary>
public class MeetingNotesResultViewModel
{
    public MeetingNoteSummaryViewModel Note { get; set; } = new();
}
