using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Services;

/// <summary>
/// Lead CRUD, search/filtering, and detail aggregation. Owns the lifecycle of
/// <see cref="Lead.CreatedAt"/> and <see cref="Lead.UpdatedAt"/>.
/// </summary>
public class LeadService : ILeadService
{
    private readonly AppDbContext _db;
    private readonly IAiAnalysisService _aiAnalysis;
    private readonly IMeetingNotesService _meetingNotes;

    public LeadService(AppDbContext db, IAiAnalysisService aiAnalysis, IMeetingNotesService meetingNotes)
    {
        _db = db;
        _aiAnalysis = aiAnalysis;
        _meetingNotes = meetingNotes;
    }

    public async Task<LeadListViewModel> GetLeadListAsync(LeadFilterViewModel filter)
    {
        IQueryable<Lead> query = _db.Leads.AsNoTracking();

        if (filter.Status.HasValue)
        {
            query = query.Where(l => l.Status == filter.Status.Value);
        }

        if (filter.LeadSource.HasValue)
        {
            query = query.Where(l => l.LeadSource == filter.LeadSource.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            var loc = filter.Location.Trim();
            query = query.Where(l => l.Location != null && l.Location.Contains(loc));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(l =>
                l.Name.Contains(term)
                || (l.Email != null && l.Email.Contains(term))
                || (l.Notes != null && l.Notes.Contains(term)));
        }

        // Project each lead together with its latest analysis (if any).
        var projected = query
            .Select(l => new
            {
                Lead = l,
                Latest = l.Analyses
                    .OrderByDescending(a => a.GeneratedAt)
                    .Select(a => new { a.LeadScore, a.UrgencyLevel })
                    .FirstOrDefault()
            });

        // MinLeadScore must be applied after the latest analysis is known.
        if (filter.MinLeadScore.HasValue)
        {
            var min = filter.MinLeadScore.Value;
            projected = projected.Where(x => x.Latest != null && x.Latest.LeadScore >= min);
        }

        var rows = await projected
            .OrderByDescending(x => x.Lead.UpdatedAt)
            .Select(x => new LeadRowViewModel
            {
                Id = x.Lead.Id,
                Name = x.Lead.Name,
                Location = x.Lead.Location,
                LeadSource = x.Lead.LeadSource,
                Status = x.Lead.Status,
                Budget = x.Lead.Budget,
                UpdatedAt = x.Lead.UpdatedAt,
                LatestLeadScore = x.Latest != null ? x.Latest.LeadScore : (int?)null,
                LatestUrgency = x.Latest != null ? x.Latest.UrgencyLevel : (Models.Enums.UrgencyLevel?)null
            })
            .ToListAsync();

        return new LeadListViewModel
        {
            Filter = filter,
            Leads = rows,
            TotalCount = rows.Count
        };
    }

    public async Task<Lead?> GetByIdAsync(int id) =>
        await _db.Leads.FirstOrDefaultAsync(l => l.Id == id);

    public async Task<LeadDetailsViewModel?> GetDetailsAsync(int id)
    {
        var lead = await _db.Leads
            .Include(l => l.Interactions)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lead is null)
        {
            return null;
        }

        var interactions = lead.Interactions
            .OrderByDescending(i => i.OccurredAt)
            .ThenByDescending(i => i.Id)
            .ToList();

        var latest = await _aiAnalysis.GetLatestAsync(id);
        var notes = await _meetingNotes.GetForLeadAsync(id);

        return new LeadDetailsViewModel
        {
            Lead = lead,
            Interactions = interactions,
            LatestAnalysis = latest,
            MeetingNotes = notes,
            NewInteraction = new InteractionFormViewModel { LeadId = id }
        };
    }

    public async Task<LeadFormViewModel?> GetFormAsync(int id)
    {
        var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        if (lead is null)
        {
            return null;
        }

        return new LeadFormViewModel
        {
            Id = lead.Id,
            Name = lead.Name,
            Email = lead.Email,
            Phone = lead.Phone,
            LeadSource = lead.LeadSource,
            Budget = lead.Budget,
            Location = lead.Location,
            Notes = lead.Notes,
            Status = lead.Status
        };
    }

    public async Task<int> CreateAsync(LeadFormViewModel form)
    {
        var now = DateTime.UtcNow;
        var lead = new Lead
        {
            Name = form.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim(),
            LeadSource = form.LeadSource,
            Budget = form.Budget,
            Location = string.IsNullOrWhiteSpace(form.Location) ? null : form.Location.Trim(),
            Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim(),
            Status = form.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();
        return lead.Id;
    }

    public async Task<bool> UpdateAsync(LeadFormViewModel form)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == form.Id);
        if (lead is null)
        {
            return false;
        }

        lead.Name = form.Name.Trim();
        lead.Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim();
        lead.Phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim();
        lead.LeadSource = form.LeadSource;
        lead.Budget = form.Budget;
        lead.Location = string.IsNullOrWhiteSpace(form.Location) ? null : form.Location.Trim();
        lead.Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();
        lead.Status = form.Status;
        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id);
        if (lead is null)
        {
            return false;
        }

        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task TouchAsync(int leadId)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId);
        if (lead is null)
        {
            return;
        }

        lead.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<(int Id, string Name)>> GetLeadOptionsAsync()
    {
        var leads = await _db.Leads
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .Select(l => new { l.Id, l.Name })
            .ToListAsync();

        return leads.Select(l => (l.Id, l.Name)).ToList();
    }
}
