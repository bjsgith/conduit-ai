using System.ComponentModel.DataAnnotations;

namespace ConduitAI.Models;

/// <summary>
/// Raw salesperson meeting notes plus the structured AI output extracted from them.
/// Key facts and risks are stored as JSON-array text to keep the schema simple.
/// </summary>
public class MeetingNote
{
    public int Id { get; set; }

    public int? LeadId { get; set; }

    [Required]
    public string RawNotes { get; set; } = string.Empty;

    [Required]
    public string StructuredSummary { get; set; } = string.Empty;

    [Required]
    public string KeyFactsJson { get; set; } = "[]";

    [Required]
    public string RisksJson { get; set; } = "[]";

    [Required]
    public string RecommendedNextAction { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [StringLength(100)]
    public string ModelName { get; set; } = string.Empty;

    [StringLength(50)]
    public string PromptVersion { get; set; } = string.Empty;

    // Navigation
    public Lead? Lead { get; set; }
}
