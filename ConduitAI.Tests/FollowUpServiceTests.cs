using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services;
using ConduitAI.ViewModels;
using Xunit;

namespace ConduitAI.Tests;

public class FollowUpServiceTests
{
    private static Lead NewLead(string name, LeadStatus status = LeadStatus.Qualified)
    {
        var now = DateTime.UtcNow;
        return new Lead
        {
            Name = name,
            LeadSource = LeadSource.Referral,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now.AddMinutes(-1)
        };
    }

    [Fact]
    public async Task CreateAsync_TrimsAction_ConvertsLocalDueTimeToUtc_AndTouchesLead()
    {
        using var db = TestDb.Create();
        var lead = NewLead("Marcus");
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        var before = lead.UpdatedAt;
        var localDueAt = new DateTime(2026, 9, 25, 14, 30, 0, DateTimeKind.Unspecified);
        var service = new FollowUpService(db);

        var id = await service.CreateAsync(new FollowUpFormViewModel
        {
            LeadId = lead.Id,
            ActionText = "  Confirm tour time.  ",
            DueAtLocal = localDueAt
        });

        var stored = await db.LeadFollowUps.FindAsync(id);
        Assert.NotNull(stored);
        Assert.Equal("Confirm tour time.", stored!.ActionText);
        Assert.Equal(DateTime.SpecifyKind(localDueAt, DateTimeKind.Local).ToUniversalTime(), stored.DueAtUtc);
        Assert.Equal(DateTimeKind.Utc, stored.DueAtUtc.Kind);
        Assert.True((await db.Leads.FindAsync(lead.Id))!.UpdatedAt > before);
    }

    [Fact]
    public async Task CreateAsync_RejectsWhitespaceActionAfterNormalization()
    {
        using var db = TestDb.Create();
        var lead = NewLead("Priya");
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        var service = new FollowUpService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new FollowUpFormViewModel
        {
            LeadId = lead.Id,
            ActionText = "   ",
            DueAtLocal = DateTime.Now.AddHours(1)
        }));
    }

    [Fact]
    public async Task RescheduleAndComplete_PersistTimestamps_AndKeepCompletedItemOnClosedLead()
    {
        using var db = TestDb.Create();
        var lead = NewLead("Thomas");
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        var service = new FollowUpService(db);
        var id = await service.CreateAsync(new FollowUpFormViewModel
        {
            LeadId = lead.Id,
            ActionText = "Ask for feedback.",
            DueAtLocal = DateTime.Now.AddHours(1)
        });
        var form = await service.GetForEditAsync(id!.Value);
        Assert.NotNull(form);

        form!.ActionText = "  Send a thank-you. ";
        form.DueAtLocal = DateTime.Now.AddDays(2);
        Assert.True(await service.RescheduleAsync(form));
        lead.Status = LeadStatus.Closed;
        await db.SaveChangesAsync();

        var completedLeadId = await service.CompleteAsync(id.Value);
        Assert.Equal(lead.Id, completedLeadId);
        var items = await service.GetForLeadAsync(lead.Id);
        var item = Assert.Single(items);
        Assert.Equal("Send a thank-you.", item.ActionText);
        Assert.NotNull(item.CompletedAtUtc);
        Assert.Null(await service.GetForEditAsync(id.Value));
        Assert.Null(await service.CompleteAsync(id.Value));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsLeadIdAndRemovesOpenOrCompletedFollowUp()
    {
        using var db = TestDb.Create();
        var lead = NewLead("Hannah", LeadStatus.Lost);
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        var service = new FollowUpService(db);
        var id = await service.CreateAsync(new FollowUpFormViewModel
        {
            LeadId = lead.Id,
            ActionText = "Revisit next year.",
            DueAtLocal = DateTime.Now.AddMonths(10)
        });

        Assert.Equal(lead.Id, await service.DeleteAsync(id!.Value));
        Assert.Empty(await service.GetForLeadAsync(lead.Id));
        Assert.Null(await service.DeleteAsync(id.Value));
    }
}
