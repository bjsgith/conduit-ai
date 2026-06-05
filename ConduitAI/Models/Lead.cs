using System.ComponentModel.DataAnnotations;
using ConduitAI.Models.Enums;

namespace ConduitAI.Models;

/// <summary>
/// Core lead record for a prospective real-estate buyer.
/// </summary>
public class Lead
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
    public LeadSource LeadSource { get; set; }

    [Range(0, 100_000_000)]
    public decimal? Budget { get; set; }

    [StringLength(150)]
    public string? Location { get; set; }

    [StringLength(8000)]
    public string? Notes { get; set; }

    [Required]
    public LeadStatus Status { get; set; } = LeadStatus.New;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<LeadInteraction> Interactions { get; set; } = new List<LeadInteraction>();
    public ICollection<LeadAnalysis> Analyses { get; set; } = new List<LeadAnalysis>();
    public ICollection<MeetingNote> MeetingNotes { get; set; } = new List<MeetingNote>();
}
