using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services;
using ConduitAI.ViewModels;
using Xunit;

namespace ConduitAI.Tests;

public class TimelineServiceTests
{
    [Fact]
    public async Task AddInteraction_PersistsAndBumpsLeadUpdatedAt()
    {
        using var db = TestDb.Create();
        var lead = new Lead { Name = "Lead", LeadSource = LeadSource.Other, Status = LeadStatus.New, CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow.AddDays(-1) };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        var originalUpdated = lead.UpdatedAt;

        var svc = new TimelineService(db);
        await Task.Delay(5);
        var occurredAt = new DateTime(2026, 6, 5, 9, 30, 0, DateTimeKind.Unspecified);
        var ok = await svc.AddInteractionAsync(new InteractionFormViewModel
        {
            LeadId = lead.Id,
            InteractionType = InteractionType.PhoneCall,
            OccurredAt = occurredAt,
            Notes = "  Spoke about Scottsdale listings.  "
        });

        Assert.True(ok);
        var stored = (await svc.GetForLeadAsync(lead.Id)).Single();
        Assert.Equal("Spoke about Scottsdale listings.", stored.Notes);
        Assert.Equal(occurredAt, stored.OccurredAt);
        Assert.True((await db.Leads.FindAsync(lead.Id))!.UpdatedAt > originalUpdated);
    }

    [Fact]
    public async Task AddInteraction_UnknownLead_ReturnsFalse()
    {
        using var db = TestDb.Create();
        var svc = new TimelineService(db);

        var ok = await svc.AddInteractionAsync(new InteractionFormViewModel
        {
            LeadId = 999,
            InteractionType = InteractionType.Note,
            Notes = "x"
        });

        Assert.False(ok);
    }

    [Fact]
    public async Task GetForLead_OrdersNewestFirst()
    {
        using var db = TestDb.Create();
        var lead = new Lead { Name = "Lead", LeadSource = LeadSource.Other, Status = LeadStatus.New, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        var svc = new TimelineService(db);

        await svc.AddInteractionAsync(new InteractionFormViewModel { LeadId = lead.Id, InteractionType = InteractionType.Email, OccurredAt = DateTime.UtcNow.AddDays(-2), Notes = "older" });
        await svc.AddInteractionAsync(new InteractionFormViewModel { LeadId = lead.Id, InteractionType = InteractionType.Email, OccurredAt = DateTime.UtcNow, Notes = "newer" });

        var items = await svc.GetForLeadAsync(lead.Id);
        Assert.Equal("newer", items[0].Notes);
        Assert.Equal("older", items[1].Notes);
    }

    [Fact]
    public async Task DeleteInteraction_ReturnsLeadId_AndRemoves()
    {
        using var db = TestDb.Create();
        var lead = new Lead { Name = "Lead", LeadSource = LeadSource.Other, Status = LeadStatus.New, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        var svc = new TimelineService(db);
        await svc.AddInteractionAsync(new InteractionFormViewModel { LeadId = lead.Id, InteractionType = InteractionType.Note, OccurredAt = DateTime.UtcNow, Notes = "n" });
        var interactionId = (await svc.GetForLeadAsync(lead.Id)).Single().Id;

        var returnedLeadId = await svc.DeleteInteractionAsync(interactionId);

        Assert.Equal(lead.Id, returnedLeadId);
        Assert.Empty(await svc.GetForLeadAsync(lead.Id));
        Assert.Null(await svc.DeleteInteractionAsync(interactionId));
    }
}
