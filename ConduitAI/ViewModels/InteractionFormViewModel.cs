using System.ComponentModel.DataAnnotations;
using ConduitAI.Models.Enums;

namespace ConduitAI.ViewModels;

/// <summary>
/// Form model for adding a timeline interaction to a lead.
/// </summary>
public class InteractionFormViewModel
{
    [Required]
    public int LeadId { get; set; }

    [Required]
    [Display(Name = "Occurred At")]
    [DataType(DataType.DateTime)]
    public DateTime OccurredAt { get; set; } = DateTime.Now;

    [Required]
    [Display(Name = "Type")]
    public InteractionType InteractionType { get; set; } = InteractionType.PhoneCall;

    [Required(ErrorMessage = "Notes are required.")]
    [StringLength(4000, MinimumLength = 1)]
    public string Notes { get; set; } = string.Empty;
}
