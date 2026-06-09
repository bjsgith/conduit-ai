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
                    .ThenByDescending(a => a.Id)
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

        bool IsActive(LeadStatus s) => s != LeadStatus.Closed && s != LeadStatus.Lost;
        var activeLeads = leads.Count(l => IsActive(l.Status));
        var pipelineValue = leads
            .Where(l => IsActive(l.Status) && l.Budget.HasValue)
            .Sum(l => l.Budget!.Value);

        var scored = leads.Where(l => l.Latest != null).Select(l => l.Latest!.LeadScore).ToList();
        var averageLeadScore = scored.Count > 0 ? (int)Math.Round(scored.Average()) : 0;

        // Counts per lifecycle stage, kept in pipeline order for the distribution bar.
        var byStatus = leads.GroupBy(l => l.Status).ToDictionary(g => g.Key, g => g.Count());
        var pipeline = Enum.GetValues<LeadStatus>()
            .Select(s => new PipelineStageViewModel { Status = s, Count = byStatus.GetValueOrDefault(s) })
            .ToList();

        var highPriority = leads.Count(l =>
            l.Latest != null &&
            l.Status != LeadStatus.Closed &&
            l.Status != LeadStatus.Lost &&
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
            ActiveLeads = activeLeads,
            PipelineValue = pipelineValue,
            AverageLeadScore = averageLeadScore,
            RecentLeads = recentLeads,
            FollowUpQueue = followUpQueue,
            Pipeline = pipeline
        };
    }
}
