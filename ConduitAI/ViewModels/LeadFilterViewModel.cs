using System.ComponentModel.DataAnnotations;
using ConduitAI.Models.Enums;

namespace ConduitAI.ViewModels;

/// <summary>
/// Filter criteria for the lead list. All values are optional and bind from the
/// query string so filtering works without JavaScript.
/// </summary>
public class LeadFilterViewModel
{
    public LeadStatus? Status { get; set; }

    public LeadSource? LeadSource { get; set; }

    [Range(0, 100)]
    [Display(Name = "Min Score")]
    public int? MinLeadScore { get; set; }

    [StringLength(150)]
    public string? Location { get; set; }

    [StringLength(150)]
    public string? Search { get; set; }

    public bool HasAnyFilter =>
        Status.HasValue || LeadSource.HasValue || MinLeadScore.HasValue
        || !string.IsNullOrWhiteSpace(Location) || !string.IsNullOrWhiteSpace(Search);
}
