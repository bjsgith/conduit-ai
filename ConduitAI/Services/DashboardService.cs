using ConduitAI.Data;
using ConduitAI.Models.Enums;
using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Services;

/// <summary>Builds compact dashboard metrics from persisted CRM data.</summary>
public class DashboardService : IDashboardService
{
    private const int HighPriorityScoreThreshold = 75;
    private const int RecentLeadsCount = 5;
    private const int FollowUpQueueCount = 6;

    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var totalLeads = await _db.Leads.CountAsync();
        var newLeads = await _db.Leads.CountAsync(l => l.Status == LeadStatus.New);
        var activeLeadsQuery = _db.Leads.Where(l => l.Status != LeadStatus.Closed && l.Status != LeadStatus.Lost);
        var activeLeads = await activeLeadsQuery.CountAsync();
        var pipelineValue = await activeLeadsQuery.SumAsync(l => l.Budget ?? 0m);

        var highPriorityLeads = await activeLeadsQuery.CountAsync(l =>
            l.Analyses
                .OrderByDescending(a => a.GeneratedAt)
                .ThenByDescending(a => a.Id)
                .Select(a => a.LeadScore)
                .FirstOrDefault() >= HighPriorityScoreThreshold
            || l.Analyses
                .OrderByDescending(a => a.GeneratedAt)
                .ThenByDescending(a => a.Id)
                .Select(a => a.UrgencyLevel)
                .FirstOrDefault() == UrgencyLevel.High);

        var latestScores = _db.Leads
            .Select(l => l.Analyses
                .OrderByDescending(a => a.GeneratedAt)
                .ThenByDescending(a => a.Id)
                .Select(a => (int?)a.LeadScore)
                .FirstOrDefault())
            .Where(score => score.HasValue);
        var scoredLeadCount = await latestScores.CountAsync();
        var averageLeadScore = scoredLeadCount == 0
            ? 0
            : (int)Math.Round(await latestScores.AverageAsync(score => (double)score!.Value));

        var byStatus = await _db.Leads
            .GroupBy(l => l.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count);
        var pipeline = Enum.GetValues<LeadStatus>()
            .Select(status => new PipelineStageViewModel
            {
                Status = status,
                Count = byStatus.GetValueOrDefault(status)
            })
            .ToList();

        var recentLeads = await _db.Leads
            .AsNoTracking()
            .OrderByDescending(l => l.UpdatedAt)
            .ThenByDescending(l => l.Id)
            .Take(RecentLeadsCount)
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
                    .ThenByDescending(a => a.Id)
                    .Select(a => new { a.LeadScore, a.UrgencyLevel })
                    .FirstOrDefault()
            })
            .Select(l => new LeadRowViewModel
            {
                Id = l.Id,
                Name = l.Name,
                Location = l.Location,
                LeadSource = l.LeadSource,
                Status = l.Status,
                Budget = l.Budget,
                UpdatedAt = l.UpdatedAt,
                LatestLeadScore = l.Latest != null ? l.Latest.LeadScore : (int?)null,
                LatestUrgency = l.Latest != null ? l.Latest.UrgencyLevel : (UrgencyLevel?)null
            })
            .ToListAsync();

        var dueBeforeUtc = DateTime.UtcNow.AddDays(7);
        var dueFollowUpsQuery = _db.LeadFollowUps
            .AsNoTracking()
            .Where(f => f.CompletedAtUtc == null && f.DueAtUtc <= dueBeforeUtc);
        var upcomingFollowUps = await dueFollowUpsQuery.CountAsync();
        var followUpQueue = await dueFollowUpsQuery
            .OrderBy(f => f.DueAtUtc)
            .ThenBy(f => f.Id)
            .Take(FollowUpQueueCount)
            .Select(f => new FollowUpItemViewModel
            {
                Id = f.Id,
                LeadId = f.LeadId,
                LeadName = f.Lead.Name,
                ActionText = f.ActionText,
                DueAtUtc = f.DueAtUtc
            })
            .ToListAsync();

        return new DashboardViewModel
        {
            TotalLeads = totalLeads,
            NewLeads = newLeads,
            HighPriorityLeads = highPriorityLeads,
            UpcomingFollowUps = upcomingFollowUps,
            ActiveLeads = activeLeads,
            PipelineValue = pipelineValue,
            AverageLeadScore = averageLeadScore,
            RecentLeads = recentLeads,
            FollowUpQueue = followUpQueue,
            Pipeline = pipeline
        };
    }
}
