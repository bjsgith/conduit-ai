using ConduitAI.Models.Enums;

namespace ConduitAI.ViewModels;

/// <summary>
/// Compact projection of a lead for table rows and dashboard lists, including
/// the latest stored AI score/urgency when available.
/// </summary>
public class LeadRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public LeadSource LeadSource { get; set; }
    public LeadStatus Status { get; set; }
    public decimal? Budget { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int? LatestLeadScore { get; set; }
    public UrgencyLevel? LatestUrgency { get; set; }
}
