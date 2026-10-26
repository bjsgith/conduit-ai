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

    [Required(ErrorMessage = "Meeting notes must contain at least 10 non-whitespace characters.")]
    [StringLength(20000, ErrorMessage = "Meeting notes must be 20000 characters or fewer.")]
    [Display(Name = "Meeting notes")]
    public string RawNotes { get; set; } = string.Empty;

    // Populated by the controller for the lead dropdown.
    public IEnumerable<SelectListItem> LeadOptions { get; set; } = new List<SelectListItem>();
}
