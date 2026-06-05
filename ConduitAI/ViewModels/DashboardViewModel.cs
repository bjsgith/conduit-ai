namespace ConduitAI.ViewModels;

/// <summary>
/// Query-driven dashboard metrics and lists. Built from stored data only;
/// no AI calls are made when rendering the dashboard.
/// </summary>
public class DashboardViewModel
{
    public int TotalLeads { get; set; }
    public int NewLeads { get; set; }
    public int HighPriorityLeads { get; set; }
    public int UpcomingFollowUps { get; set; }

    public IReadOnlyList<LeadRowViewModel> RecentLeads { get; set; } = new List<LeadRowViewModel>();
    public IReadOnlyList<FollowUpItemViewModel> FollowUpQueue { get; set; } = new List<FollowUpItemViewModel>();
}

/// <summary>
/// A single follow-up queue row derived from a lead's latest AI recommendation.
/// </summary>
public class FollowUpItemViewModel
{
    public int LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string RecommendedNextAction { get; set; } = string.Empty;
    public Models.Enums.UrgencyLevel Urgency { get; set; }
    public DateTime GeneratedAt { get; set; }
}
