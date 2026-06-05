using System.ComponentModel.DataAnnotations;
using ConduitAI.Models.Enums;

namespace ConduitAI.Models;

/// <summary>
/// Stored AI-generated lead intelligence. Generated only on explicit user action;
/// history is retained rather than overwritten.
/// </summary>
public class LeadAnalysis
{
    public int Id { get; set; }

    public int LeadId { get; set; }

    [Required]
    public string Summary { get; set; } = string.Empty;

    [Range(0, 100)]
    public int LeadScore { get; set; }

    public UrgencyLevel UrgencyLevel { get; set; }

    public BuyingIntent BuyingIntent { get; set; }

    [Required]
    public string RecommendedNextAction { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    [StringLength(100)]
    public string ModelName { get; set; } = string.Empty;

    [StringLength(50)]
    public string PromptVersion { get; set; } = string.Empty;

    // Navigation
    public Lead Lead { get; set; } = null!;
}
