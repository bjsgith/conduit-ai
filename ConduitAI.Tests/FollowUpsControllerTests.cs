using ConduitAI.Controllers;
using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services;
using ConduitAI.Services.Ai;
using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ConduitAI.Tests;

public class FollowUpsControllerTests
{
    [Fact]
    public async Task Create_GetPrefillsFromStoredLeadRecommendationAndIgnoresQueryText()
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

        const string serverRecommendation = "Send three suitable listings.";
        var leads = new LeadService(
            db,
            new RecommendationAiAnalysisService(serverRecommendation),
            new StubMeetingNotesService());
        var controller = new FollowUpsController(new FollowUpService(db), leads);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.QueryString = new QueryString("?actionText=Browser-supplied%20text");
        controller.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), controller.ModelState);

        var result = Assert.IsType<ViewResult>(await controller.Create(lead.Id));
        var form = Assert.IsType<FollowUpFormViewModel>(result.Model);

        Assert.Equal("Form", result.ViewName);
        Assert.Equal(lead.Id, form.LeadId);
        Assert.Equal(serverRecommendation, form.ActionText);
        Assert.NotEqual("Browser-supplied text", form.ActionText);
    }

    private sealed class RecommendationAiAnalysisService : IAiAnalysisService
    {
        private readonly string _recommendation;

        public RecommendationAiAnalysisService(string recommendation) => _recommendation = recommendation;

        public Task<AiAnalysisViewModel?> GetLatestAsync(int leadId) =>
            Task.FromResult<AiAnalysisViewModel?>(new AiAnalysisViewModel
            {
                LeadId = leadId,
                RecommendedNextAction = _recommendation
            });

        public Task<AiOperationResult<AiAnalysisViewModel>> GenerateAsync(int leadId, CancellationToken ct = default) =>
            Task.FromResult(AiOperationResult<AiAnalysisViewModel>.Fail("not used"));
    }
}
