using ConduitAI.Data;
using ConduitAI.Models.Enums;
using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Services;

/// <summary>
/// Builds dashboard metrics and lists entirely from stored data. No AI calls
/// are made here; it reads the latest stored analysis per lead.
/// </summary>
public class DashboardService : IDashboardService
{
    private const int HighPriorityScoreThreshold = 75;
    private const int RecentLeadsCount = 5;
    private const int FollowUpQueueCount = 6;

    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        // Project each lead with its latest analysis once, then compute in memory.
        var leads = await _db.Leads
            .AsNoTracking()
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Location,
                l.LeadSource,
                l.Status,
                l.Budget,
                l.UpdatedAt,
                Latest = l.Analyses
                    .OrderByDescending(a => a.GeneratedAt)
                    .Select(a => new
                    {
                        a.LeadScore,
                        a.UrgencyLevel,
                        a.RecommendedNextAction,
                        a.GeneratedAt
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var totalLeads = leads.Count;
        var newLeads = leads.Count(l => l.Status == LeadStatus.New);

        var highPriority = leads.Count(l =>
            l.Latest != null &&
            (l.Latest.LeadScore >= HighPriorityScoreThreshold || l.Latest.UrgencyLevel == UrgencyLevel.High));

        var followUps = leads
            .Where(l =>
                l.Latest != null &&
                !string.IsNullOrWhiteSpace(l.Latest.RecommendedNextAction) &&
                l.Status != LeadStatus.Closed &&
                l.Status != LeadStatus.Lost)
            .ToList();

        var recentLeads = leads
            .OrderByDescending(l => l.UpdatedAt)
            .Take(RecentLeadsCount)
            .Select(l => new LeadRowViewModel
            {
                Id = l.Id,
                Name = l.Name,
                Location = l.Location,
                LeadSource = l.LeadSource,
                Status = l.Status,
                Budget = l.Budget,
                UpdatedAt = l.UpdatedAt,
                LatestLeadScore = l.Latest?.LeadScore,
                LatestUrgency = l.Latest?.UrgencyLevel
            })
            .ToList();

        var followUpQueue = followUps
            .OrderByDescending(l => l.Latest!.UrgencyLevel)
            .ThenByDescending(l => l.Latest!.GeneratedAt)
            .Take(FollowUpQueueCount)
            .Select(l => new FollowUpItemViewModel
            {
                LeadId = l.Id,
                LeadName = l.Name,
                RecommendedNextAction = l.Latest!.RecommendedNextAction,
                Urgency = l.Latest!.UrgencyLevel,
                GeneratedAt = l.Latest!.GeneratedAt
            })
            .ToList();

        return new DashboardViewModel
        {
            TotalLeads = totalLeads,
            NewLeads = newLeads,
            HighPriorityLeads = highPriority,
            UpcomingFollowUps = followUps.Count,
            RecentLeads = recentLeads,
            FollowUpQueue = followUpQueue
        };
    }
}
