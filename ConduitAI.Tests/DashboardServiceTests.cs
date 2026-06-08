using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services;
using Xunit;

namespace ConduitAI.Tests;

public class DashboardServiceTests
{
    private static Lead Lead(string name, LeadStatus status, DateTime updated) => new()
    {
        Name = name,
        LeadSource = LeadSource.Website,
        Status = status,
        CreatedAt = updated,
        UpdatedAt = updated
    };

    private static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;

        var newLead = Lead("New Lead", LeadStatus.New, now.AddHours(-1));
        var qualifiedHot = Lead("Qualified Hot", LeadStatus.Qualified, now.AddHours(-2));   // score 88 -> high priority + follow-up
        var contactedUrgent = Lead("Contacted Urgent", LeadStatus.Contacted, now.AddHours(-3)); // urgency High -> high priority + follow-up
        var lostLead = Lead("Lost Lead", LeadStatus.Lost, now.AddHours(-4));                // has analysis but excluded from follow-ups
        var coldLead = Lead("Cold Lead", LeadStatus.Contacted, now.AddHours(-5));           // low score, not high priority

        db.Leads.AddRange(newLead, qualifiedHot, contactedUrgent, lostLead, coldLead);
        await db.SaveChangesAsync();

        db.LeadAnalyses.AddRange(
            new LeadAnalysis { LeadId = qualifiedHot.Id, Summary = "s", LeadScore = 88, UrgencyLevel = UrgencyLevel.Medium, BuyingIntent = BuyingIntent.High, RecommendedNextAction = "Tour soon.", GeneratedAt = now.AddHours(-2), ModelName = "m", PromptVersion = "v" },
            new LeadAnalysis { LeadId = contactedUrgent.Id, Summary = "s", LeadScore = 60, UrgencyLevel = UrgencyLevel.High, BuyingIntent = BuyingIntent.Medium, RecommendedNextAction = "Call back.", GeneratedAt = now.AddHours(-3), ModelName = "m", PromptVersion = "v" },
            new LeadAnalysis { LeadId = lostLead.Id, Summary = "s", LeadScore = 80, UrgencyLevel = UrgencyLevel.High, BuyingIntent = BuyingIntent.Low, RecommendedNextAction = "Re-engage later.", GeneratedAt = now.AddHours(-4), ModelName = "m", PromptVersion = "v" },
            new LeadAnalysis { LeadId = coldLead.Id, Summary = "s", LeadScore = 30, UrgencyLevel = UrgencyLevel.Low, BuyingIntent = BuyingIntent.Low, RecommendedNextAction = "Nurture.", GeneratedAt = now.AddHours(-5), ModelName = "m", PromptVersion = "v" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetDashboard_ComputesMetrics()
    {
        using var db = TestDb.Create();
        await SeedAsync(db);
        var svc = new DashboardService(db);

        var vm = await svc.GetDashboardAsync();

        Assert.Equal(5, vm.TotalLeads);
        Assert.Equal(1, vm.NewLeads);
        // High priority excludes closed/lost leads even when they have high score/urgency.
        Assert.Equal(2, vm.HighPriorityLeads);
        // Follow-ups: qualifiedHot + contactedUrgent + coldLead (have next action, not Closed/Lost) = 3
        Assert.Equal(3, vm.UpcomingFollowUps);
    }

    [Fact]
    public async Task GetDashboard_RecentLeads_OrderedByUpdatedDesc_MaxFive()
    {
        using var db = TestDb.Create();
        await SeedAsync(db);
        var svc = new DashboardService(db);

        var vm = await svc.GetDashboardAsync();

        Assert.True(vm.RecentLeads.Count <= 5);
        Assert.Equal("New Lead", vm.RecentLeads[0].Name);
    }

    [Fact]
    public async Task GetDashboard_FollowUpQueue_ExcludesClosedAndLost()
    {
        using var db = TestDb.Create();
        await SeedAsync(db);
        var svc = new DashboardService(db);

        var vm = await svc.GetDashboardAsync();

        Assert.DoesNotContain(vm.FollowUpQueue, f => f.LeadName == "Lost Lead");
        Assert.Contains(vm.FollowUpQueue, f => f.LeadName == "Qualified Hot");
    }

    [Fact]
    public async Task GetDashboard_LatestAnalysis_TiesByIdDescending()
    {
        using var db = TestDb.Create();
        var now = DateTime.UtcNow;
        var lead = Lead("Tie Lead", LeadStatus.Qualified, now);
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        db.LeadAnalyses.AddRange(
            new LeadAnalysis { LeadId = lead.Id, Summary = "old", LeadScore = 30, UrgencyLevel = UrgencyLevel.Low, BuyingIntent = BuyingIntent.Low, RecommendedNextAction = "old", GeneratedAt = now, ModelName = "m", PromptVersion = "v" },
            new LeadAnalysis { LeadId = lead.Id, Summary = "new", LeadScore = 90, UrgencyLevel = UrgencyLevel.High, BuyingIntent = BuyingIntent.High, RecommendedNextAction = "new", GeneratedAt = now, ModelName = "m", PromptVersion = "v" });
        await db.SaveChangesAsync();
        var svc = new DashboardService(db);

        var vm = await svc.GetDashboardAsync();

        Assert.Single(vm.RecentLeads);
        Assert.Equal(90, vm.RecentLeads[0].LatestLeadScore);
        Assert.Single(vm.FollowUpQueue);
        Assert.Equal("new", vm.FollowUpQueue[0].RecommendedNextAction);
    }

    [Fact]
    public async Task GetDashboard_EmptyDatabase_ReturnsZeros()
    {
        using var db = TestDb.Create();
        var svc = new DashboardService(db);

        var vm = await svc.GetDashboardAsync();

        Assert.Equal(0, vm.TotalLeads);
        Assert.Empty(vm.RecentLeads);
        Assert.Empty(vm.FollowUpQueue);
    }
}
