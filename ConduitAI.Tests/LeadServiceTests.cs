using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services;
using ConduitAI.ViewModels;
using Xunit;

namespace ConduitAI.Tests;

public class LeadServiceTests
{
    private static LeadService NewService(AppDbContext db) =>
        new(db, new StubAiAnalysisService(), new StubMeetingNotesService());

    private static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        db.Leads.AddRange(
            new Lead { Name = "Marcus Whitfield", Location = "Scottsdale, AZ", LeadSource = LeadSource.Referral, Status = LeadStatus.Qualified, Email = "marcus@example.com", CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-1) },
            new Lead { Name = "Priya Nair", Location = "Tempe, AZ", LeadSource = LeadSource.Zillow, Status = LeadStatus.New, CreatedAt = now.AddDays(-2), UpdatedAt = now.AddHours(-5) },
            new Lead { Name = "Sofia Alvarez", Location = "Scottsdale, AZ", LeadSource = LeadSource.Website, Status = LeadStatus.New, CreatedAt = now.AddDays(-1), UpdatedAt = now });
        await db.SaveChangesAsync();

        // Give Marcus a high-score analysis for MinLeadScore filtering.
        var marcus = db.Leads.First(l => l.Name == "Marcus Whitfield");
        db.LeadAnalyses.Add(new LeadAnalysis
        {
            LeadId = marcus.Id,
            Summary = "Strong buyer.",
            LeadScore = 88,
            UrgencyLevel = UrgencyLevel.High,
            BuyingIntent = BuyingIntent.High,
            RecommendedNextAction = "Tour soon.",
            GeneratedAt = now.AddHours(-2),
            ModelName = "seed",
            PromptVersion = "seed"
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_SetsTimestampsAndTrimsInput()
    {
        using var db = TestDb.Create();
        var svc = NewService(db);

        var id = await svc.CreateAsync(new LeadFormViewModel
        {
            Name = "  Grace Lin  ",
            Email = " grace@example.com ",
            LeadSource = LeadSource.OpenHouse,
            Status = LeadStatus.Contacted
        });

        var lead = await svc.GetByIdAsync(id);
        Assert.NotNull(lead);
        Assert.Equal("Grace Lin", lead!.Name);
        Assert.Equal("grace@example.com", lead.Email);
        Assert.NotEqual(default, lead.CreatedAt);
        Assert.Equal(lead.CreatedAt, lead.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_ChangesFields_AndBumpsUpdatedAt()
    {
        using var db = TestDb.Create();
        var svc = NewService(db);
        var id = await svc.CreateAsync(new LeadFormViewModel { Name = "Old Name", LeadSource = LeadSource.Other, Status = LeadStatus.New });

        var before = (await svc.GetByIdAsync(id))!;
        before.UpdatedAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        var originalUpdated = before.UpdatedAt;

        var ok = await svc.UpdateAsync(new LeadFormViewModel
        {
            Id = id,
            Name = "New Name",
            LeadSource = LeadSource.Referral,
            Status = LeadStatus.Qualified
        });

        Assert.True(ok);
        var after = (await svc.GetByIdAsync(id))!;
        Assert.Equal("New Name", after.Name);
        Assert.Equal(LeadStatus.Qualified, after.Status);
        Assert.True(after.UpdatedAt > originalUpdated);
    }

    [Fact]
    public async Task DeleteAsync_RemovesLead()
    {
        using var db = TestDb.Create();
        var svc = NewService(db);
        var id = await svc.CreateAsync(new LeadFormViewModel { Name = "Temp", LeadSource = LeadSource.Other, Status = LeadStatus.New });

        Assert.True(await svc.DeleteAsync(id));
        Assert.Null(await svc.GetByIdAsync(id));
        Assert.False(await svc.DeleteAsync(id));
    }

    [Fact]
    public async Task GetLeadList_FilterByStatus()
    {
        using var db = TestDb.Create();
        await SeedAsync(db);
        var svc = NewService(db);

        var result = await svc.GetLeadListAsync(new LeadFilterViewModel { Status = LeadStatus.New });

        Assert.Equal(2, result.Leads.Count);
        Assert.All(result.Leads, l => Assert.Equal(LeadStatus.New, l.Status));
    }

    [Fact]
    public async Task GetLeadList_FilterBySource()
    {
        using var db = TestDb.Create();
        await SeedAsync(db);
        var svc = NewService(db);

        var result = await svc.GetLeadListAsync(new LeadFilterViewModel { LeadSource = LeadSource.Zillow });

        Assert.Single(result.Leads);
        Assert.Equal("Priya Nair", result.Leads[0].Name);
    }

    [Fact]
    public async Task GetLeadList_FilterByLocation_IsSubstring()
    {
        using var db = TestDb.Create();
        await SeedAsync(db);
        var svc = NewService(db);

        var result = await svc.GetLeadListAsync(new LeadFilterViewModel { Location = "Scottsdale" });

        Assert.Equal(2, result.Leads.Count);
    }

    [Fact]
    public async Task GetLeadList_FilterBySearch_MatchesNameOrEmail()
    {
        using var db = TestDb.Create();
        await SeedAsync(db);
        var svc = NewService(db);

        var byName = await svc.GetLeadListAsync(new LeadFilterViewModel { Search = "Priya" });
        Assert.Single(byName.Leads);

        var byEmail = await svc.GetLeadListAsync(new LeadFilterViewModel { Search = "marcus@" });
        Assert.Single(byEmail.Leads);
    }

    [Fact]
    public async Task GetLeadList_FilterByMinScore_UsesLatestAnalysis()
    {
        using var db = TestDb.Create();
        await SeedAsync(db);
        var svc = NewService(db);

        var result = await svc.GetLeadListAsync(new LeadFilterViewModel { MinLeadScore = 75 });

        Assert.Single(result.Leads);
        Assert.Equal("Marcus Whitfield", result.Leads[0].Name);
        Assert.Equal(88, result.Leads[0].LatestLeadScore);
    }

    [Fact]
    public async Task GetLeadList_LatestAnalysis_TiesByIdDescending()
    {
        using var db = TestDb.Create();
        var now = DateTime.UtcNow;
        var lead = new Lead { Name = "Tie Lead", LeadSource = LeadSource.Website, Status = LeadStatus.New, CreatedAt = now, UpdatedAt = now };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        db.LeadAnalyses.AddRange(
            new LeadAnalysis { LeadId = lead.Id, Summary = "old", LeadScore = 40, UrgencyLevel = UrgencyLevel.Low, BuyingIntent = BuyingIntent.Low, RecommendedNextAction = "old", GeneratedAt = now, ModelName = "m", PromptVersion = "v" },
            new LeadAnalysis { LeadId = lead.Id, Summary = "new", LeadScore = 90, UrgencyLevel = UrgencyLevel.High, BuyingIntent = BuyingIntent.High, RecommendedNextAction = "new", GeneratedAt = now, ModelName = "m", PromptVersion = "v" });
        await db.SaveChangesAsync();
        var svc = NewService(db);

        var result = await svc.GetLeadListAsync(new LeadFilterViewModel());

        Assert.Single(result.Leads);
        Assert.Equal(90, result.Leads[0].LatestLeadScore);
        Assert.Equal(UrgencyLevel.High, result.Leads[0].LatestUrgency);
    }

    [Fact]
    public async Task GetLeadList_OrdersByUpdatedAtDescending()
    {
        using var db = TestDb.Create();
        await SeedAsync(db);
        var svc = NewService(db);

        var result = await svc.GetLeadListAsync(new LeadFilterViewModel());

        Assert.Equal(3, result.Leads.Count);
        Assert.Equal("Sofia Alvarez", result.Leads[0].Name);
    }
}
