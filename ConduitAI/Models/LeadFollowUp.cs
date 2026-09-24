using System.ComponentModel.DataAnnotations;

namespace ConduitAI.Models;

/// <summary>A scheduled action to complete for a lead.</summary>
public class LeadFollowUp
{
    public int Id { get; set; }

    public int LeadId { get; set; }

    [Required]
    [StringLength(500)]
    public string ActionText { get; set; } = string.Empty;

    public DateTime DueAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Lead Lead { get; set; } = null!;
}
