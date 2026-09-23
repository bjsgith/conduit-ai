using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services;
using ConduitAI.ViewModels;
using ConduitAI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ConduitAI.Tests;

public class InteractionsControllerTests
{
    [Fact]
    public async Task InvalidCreate_RedisplaysSubmittedValuesAndValidationErrorsOnLeadDetails()
    {
        using var db = TestDb.Create();
        var lead = new Lead
        {
            Name = "Sofia Alvarez",
            LeadSource = LeadSource.Website,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        var leads = new LeadService(db, new StubAiAnalysisService(), new StubMeetingNotesService());
        var controller = new InteractionsController(new TimelineService(db), leads, new FollowUpService(db));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), controller.ModelState);
        controller.ModelState.AddModelError("NewInteraction.Notes", "Notes are required.");
        var submittedAt = new DateTime(2026, 9, 23, 10, 45, 0, DateTimeKind.Unspecified);
        var form = new InteractionFormViewModel
        {
            LeadId = lead.Id,
            OccurredAt = submittedAt,
            InteractionType = InteractionType.PropertyTour,
            Notes = "   "
        };

        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(await controller.Create(form));
        var model = Assert.IsType<LeadDetailsViewModel>(result.Model);

        Assert.Equal("~/Views/Leads/Details.cshtml", result.ViewName);
        Assert.Equal("   ", model.NewInteraction.Notes);
        Assert.Equal(submittedAt, model.NewInteraction.OccurredAt);
        Assert.Equal(InteractionType.PropertyTour, model.NewInteraction.InteractionType);
        Assert.True(controller.ModelState.ContainsKey("NewInteraction.Notes"));
    }
}
