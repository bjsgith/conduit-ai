using System.ComponentModel.DataAnnotations;
using ConduitAI.Models.Enums;

namespace ConduitAI.Models;

/// <summary>
/// A single timeline event recorded against a lead.
/// </summary>
public class LeadInteraction
{
    public int Id { get; set; }

    public int LeadId { get; set; }

    public DateTime OccurredAt { get; set; }

    [Required]
    public InteractionType InteractionType { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Lead Lead { get; set; } = null!;
}
