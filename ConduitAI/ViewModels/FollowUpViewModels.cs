using System.ComponentModel.DataAnnotations;

namespace ConduitAI.ViewModels;

public class FollowUpFormViewModel
{
    public int? Id { get; set; }

    [Required]
    public int LeadId { get; set; }

    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a follow-up action.")]
    [StringLength(500, MinimumLength = 1)]
    [Display(Name = "Action")]
    public string ActionText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a due date and time.")]
    [Display(Name = "Due date and time")]
    [DataType(DataType.DateTime)]
    public DateTime? DueAtLocal { get; set; }
}

public class LeadFollowUpViewModel
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public DateTime DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
