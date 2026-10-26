namespace ConduitAI.ViewModels;

/// <summary>
/// Backing model for the lead list page: the applied filter plus matching rows.
/// </summary>
public class LeadListViewModel
{
    public LeadFilterViewModel Filter { get; set; } = new();
    public IReadOnlyList<LeadRowViewModel> Leads { get; set; } = new List<LeadRowViewModel>();
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 25;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
