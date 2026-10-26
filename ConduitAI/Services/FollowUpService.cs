using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Services;

public class FollowUpService : IFollowUpService
{
    private readonly AppDbContext _db;

    public FollowUpService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeadFollowUpViewModel>> GetForLeadAsync(int leadId) =>
        await _db.LeadFollowUps
            .AsNoTracking()
            .Where(f => f.LeadId == leadId)
            .OrderBy(f => f.CompletedAtUtc.HasValue)
            .ThenBy(f => f.DueAtUtc)
            .ThenBy(f => f.Id)
            .Select(f => new LeadFollowUpViewModel
            {
                Id = f.Id,
                LeadId = f.LeadId,
                LeadName = f.Lead.Name,
                ActionText = f.ActionText,
                DueAtUtc = f.DueAtUtc,
                CompletedAtUtc = f.CompletedAtUtc
            })
            .ToListAsync();

    public async Task<int?> CreateAsync(FollowUpFormViewModel form)
    {
        Validate(form);
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == form.LeadId);
        if (lead is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var followUp = new LeadFollowUp
        {
            LeadId = lead.Id,
            ActionText = form.ActionText.Trim(),
            DueAtUtc = ToUtc(form.DueAtLocal!.Value),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.LeadFollowUps.Add(followUp);
        lead.UpdatedAt = now;
        await _db.SaveChangesAsync();
        return followUp.Id;
    }

    public async Task<FollowUpFormViewModel?> GetForEditAsync(int id)
    {
        var followUp = await _db.LeadFollowUps
            .AsNoTracking()
            .Include(f => f.Lead)
            .FirstOrDefaultAsync(f => f.Id == id && f.CompletedAtUtc == null);

        if (followUp is null)
        {
            return null;
        }

        return new FollowUpFormViewModel
        {
            Id = followUp.Id,
            LeadId = followUp.LeadId,
            LeadName = followUp.Lead.Name,
            ActionText = followUp.ActionText,
            DueAtLocal = followUp.DueAtUtc.ToLocalTime()
        };
    }

    public async Task<bool> RescheduleAsync(FollowUpFormViewModel form)
    {
        Validate(form);
        if (!form.Id.HasValue)
        {
            return false;
        }

        var followUp = await _db.LeadFollowUps
            .Include(f => f.Lead)
            .FirstOrDefaultAsync(f => f.Id == form.Id.Value && f.LeadId == form.LeadId && f.CompletedAtUtc == null);
        if (followUp is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        followUp.ActionText = form.ActionText.Trim();
        followUp.DueAtUtc = ToUtc(form.DueAtLocal!.Value);
        followUp.UpdatedAt = now;
        followUp.Lead.UpdatedAt = now;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int?> CompleteAsync(int id)
    {
        var followUp = await _db.LeadFollowUps
            .Include(f => f.Lead)
            .FirstOrDefaultAsync(f => f.Id == id && f.CompletedAtUtc == null);
        if (followUp is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        followUp.CompletedAtUtc = now;
        followUp.UpdatedAt = now;
        followUp.Lead.UpdatedAt = now;
        await _db.SaveChangesAsync();
        return followUp.LeadId;
    }

    public async Task<int?> DeleteAsync(int id)
    {
        var followUp = await _db.LeadFollowUps
            .Include(f => f.Lead)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (followUp is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        followUp.Lead.UpdatedAt = now;
        _db.LeadFollowUps.Remove(followUp);
        await _db.SaveChangesAsync();
        return followUp.LeadId;
    }

    private static void Validate(FollowUpFormViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.ActionText) || !form.DueAtLocal.HasValue || form.DueAtLocal.Value == default)
        {
            throw new ArgumentException("A follow-up action and due date are required.");
        }
    }

    private static DateTime ToUtc(DateTime localTime) =>
        DateTime.SpecifyKind(localTime, DateTimeKind.Local).ToUniversalTime();
}
