using ConduitAI.Models.Enums;

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

    /// <summary>Leads that are neither Closed nor Lost.</summary>
    public int ActiveLeads { get; set; }

    /// <summary>Sum of stated budgets across active leads; an at-a-glance pipeline value.</summary>
    public decimal PipelineValue { get; set; }

    /// <summary>Average latest AI lead score across leads that have been analyzed (0 when none).</summary>
    public int AverageLeadScore { get; set; }

    public IReadOnlyList<LeadRowViewModel> RecentLeads { get; set; } = new List<LeadRowViewModel>();
    public IReadOnlyList<FollowUpItemViewModel> FollowUpQueue { get; set; } = new List<FollowUpItemViewModel>();

    /// <summary>Lead counts per lifecycle stage, ordered by the pipeline, for the distribution bar.</summary>
    public IReadOnlyList<PipelineStageViewModel> Pipeline { get; set; } = new List<PipelineStageViewModel>();
}

/// <summary>
/// A single follow-up queue row derived from a lead's latest AI recommendation.
/// </summary>
public class FollowUpItemViewModel
{
    public int LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string RecommendedNextAction { get; set; } = string.Empty;
    public UrgencyLevel Urgency { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// One lifecycle stage with its lead count, used to render the pipeline distribution.
/// </summary>
public class PipelineStageViewModel
{
    public LeadStatus Status { get; set; }
    public int Count { get; set; }
}
