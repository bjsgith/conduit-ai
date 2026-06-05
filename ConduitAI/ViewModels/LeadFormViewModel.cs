using System.ComponentModel.DataAnnotations;
using ConduitAI.Models.Enums;

namespace ConduitAI.ViewModels;

/// <summary>
/// Focused create/edit form model. Prevents over-posting by exposing only the
/// fields a user is allowed to set; timestamps are managed by the service layer.
/// </summary>
public class LeadFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(50)]
    [Phone]
    public string? Phone { get; set; }

    [Required]
    [Display(Name = "Lead Source")]
    public LeadSource LeadSource { get; set; }

    [Range(0, 100_000_000, ErrorMessage = "Budget must be a positive amount.")]
    public decimal? Budget { get; set; }

    [StringLength(150)]
    public string? Location { get; set; }

    [StringLength(8000)]
    public string? Notes { get; set; }

    [Required]
    public LeadStatus Status { get; set; } = LeadStatus.New;
}
