namespace ConduitAI.ViewModels;

/// <summary>
/// Backing model for the lead list page: the applied filter plus matching rows.
/// </summary>
public class LeadListViewModel
{
    public LeadFilterViewModel Filter { get; set; } = new();
    public IReadOnlyList<LeadRowViewModel> Leads { get; set; } = new List<LeadRowViewModel>();
    public int TotalCount { get; set; }
}
