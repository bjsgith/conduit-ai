using System.ComponentModel.DataAnnotations;
using ConduitAI.Models.Enums;
using ConduitAI.ViewModels;
using Xunit;

namespace ConduitAI.Tests;

public class ViewModelValidationTests
{
    [Fact]
    public void LeadForm_InvalidLeadSource_FailsValidation()
    {
        var form = new LeadFormViewModel
        {
            Name = "Lead",
            LeadSource = (LeadSource)999,
            Status = LeadStatus.New
        };

        Assert.False(IsValid(form));
    }

    [Fact]
    public void LeadForm_InvalidStatus_FailsValidation()
    {
        var form = new LeadFormViewModel
        {
            Name = "Lead",
            LeadSource = LeadSource.Referral,
            Status = (LeadStatus)999
        };

        Assert.False(IsValid(form));
    }

    [Fact]
    public void InteractionForm_InvalidType_FailsValidation()
    {
        var form = new InteractionFormViewModel
        {
            LeadId = 1,
            OccurredAt = new DateTime(2026, 6, 5, 9, 30, 0),
            InteractionType = (InteractionType)999,
            Notes = "Discussed property tour."
        };

        Assert.False(IsValid(form));
    }

    private static bool IsValid(object model)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
    }
}
