using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Services;

/// <summary>
/// Manages a lead's interaction timeline. Adding or removing entries also bumps
/// the parent lead's <see cref="Lead.UpdatedAt"/> to reflect recent activity.
/// </summary>
public class TimelineService : ITimelineService
{
    private readonly AppDbContext _db;

    public TimelineService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LeadInteraction>> GetForLeadAsync(int leadId)
    {
        return await _db.LeadInteractions
            .AsNoTracking()
            .Where(i => i.LeadId == leadId)
            .OrderByDescending(i => i.OccurredAt)
            .ThenByDescending(i => i.Id)
            .ToListAsync();
    }

    public async Task<bool> AddInteractionAsync(InteractionFormViewModel form)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == form.LeadId);
        if (lead is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var interaction = new LeadInteraction
        {
            LeadId = form.LeadId,
            // The form posts a local wall-clock time (datetime-local input). Store
            // it as UTC so all timestamps share one frame; views convert back.
            OccurredAt = DateTime.SpecifyKind(form.OccurredAt, DateTimeKind.Local).ToUniversalTime(),
            InteractionType = form.InteractionType,
            Notes = form.Notes.Trim(),
            CreatedAt = now
        };

        _db.LeadInteractions.Add(interaction);
        lead.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int?> DeleteInteractionAsync(int interactionId)
    {
        var interaction = await _db.LeadInteractions.FirstOrDefaultAsync(i => i.Id == interactionId);
        if (interaction is null)
        {
            return null;
        }

        var leadId = interaction.LeadId;
        _db.LeadInteractions.Remove(interaction);

        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId);
        if (lead is not null)
        {
            lead.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return leadId;
    }
}
