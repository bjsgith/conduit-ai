using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConduitAI.ViewModels;

/// <summary>
/// Form model for the meeting notes assistant. Optionally attaches the result
/// to an existing lead.
/// </summary>
public class MeetingNotesFormViewModel
{
    [Display(Name = "Attach to lead (optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Paste the meeting notes to analyze.")]
    [StringLength(20000, MinimumLength = 10, ErrorMessage = "Notes must be between 10 and 20000 characters.")]
    [Display(Name = "Meeting notes")]
    public string RawNotes { get; set; } = string.Empty;

    // Populated by the controller for the lead dropdown.
    public IEnumerable<SelectListItem> LeadOptions { get; set; } = new List<SelectListItem>();
}
