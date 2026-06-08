using ConduitAI.Data;
using ConduitAI.Models;
using ConduitAI.Models.Enums;
using ConduitAI.Services;
using ConduitAI.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConduitAI.Tests;

public class SqliteIntegrationTests
{
    [Fact]
    public async Task Migrate_CreatesSchemaAndLeadCanRoundTrip()
    {
        using var fixture = await SqliteDbFixture.CreateAsync();
        var svc = NewLeadService(fixture.Db);

        var id = await svc.CreateAsync(new LeadFormViewModel
        {
            Name = "SQLite Lead",
            LeadSource = LeadSource.Website,
            Status = LeadStatus.New
        });

        var fetched = await svc.GetByIdAsync(id);

        Assert.NotNull(fetched);
        Assert.Equal("SQLite Lead", fetched!.Name);
    }

    [Fact]
    public async Task DeleteLead_CascadesAttachedMeetingNotes_AndKeepsStandaloneNotes()
    {
        using var fixture = await SqliteDbFixture.CreateAsync();
        var now = DateTime.UtcNow;
        var lead = new Lead
        {
            Name = "Delete Me",
            LeadSource = LeadSource.Referral,
            Status = LeadStatus.Qualified,
            CreatedAt = now,
            UpdatedAt = now
        };
        fixture.Db.Leads.Add(lead);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.MeetingNotes.AddRange(
            Note(lead.Id, "Attached note"),
            Note(null, "Standalone note"));
        await fixture.Db.SaveChangesAsync();

        var svc = NewLeadService(fixture.Db);
        var deleted = await svc.DeleteAsync(lead.Id);

        Assert.True(deleted);
        var notes = await fixture.Db.MeetingNotes.AsNoTracking().ToListAsync();
        Assert.Single(notes);
        Assert.Null(notes[0].LeadId);
        Assert.Equal("Standalone note", notes[0].RawNotes);
    }

    [Fact]
    public async Task Services_QueryThroughSqliteMigrations()
    {
        using var fixture = await SqliteDbFixture.CreateAsync();
        var now = DateTime.UtcNow;
        var lead = new Lead
        {
            Name = "Dashboard Lead",
            LeadSource = LeadSource.OpenHouse,
            Status = LeadStatus.Qualified,
            CreatedAt = now,
            UpdatedAt = now
        };
        fixture.Db.Leads.Add(lead);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.LeadAnalyses.Add(new LeadAnalysis
        {
            LeadId = lead.Id,
            Summary = "Strong lead.",
            LeadScore = 90,
            UrgencyLevel = UrgencyLevel.High,
            BuyingIntent = BuyingIntent.High,
            RecommendedNextAction = "Call today.",
            GeneratedAt = now,
            ModelName = "test",
            PromptVersion = "test"
        });
        await fixture.Db.SaveChangesAsync();

        var list = await NewLeadService(fixture.Db).GetLeadListAsync(new LeadFilterViewModel { MinLeadScore = 80 });
        var dashboard = await new DashboardService(fixture.Db).GetDashboardAsync();

        Assert.Single(list.Leads);
        Assert.Equal(1, dashboard.HighPriorityLeads);
        Assert.Single(dashboard.FollowUpQueue);
    }

    private static LeadService NewLeadService(AppDbContext db) =>
        new(db, new StubAiAnalysisService(), new StubMeetingNotesService());

    private static MeetingNote Note(int? leadId, string rawNotes) => new()
    {
        LeadId = leadId,
        RawNotes = rawNotes,
        StructuredSummary = "Summary",
        KeyFactsJson = "[]",
        RisksJson = "[]",
        RecommendedNextAction = "Follow up.",
        CreatedAt = DateTime.UtcNow,
        ModelName = "test",
        PromptVersion = "test"
    };

    private sealed class SqliteDbFixture : IDisposable
    {
        private readonly string _directory;

        private SqliteDbFixture(string directory, AppDbContext db)
        {
            _directory = directory;
            Db = db;
        }

        public AppDbContext Db { get; }

        public static async Task<SqliteDbFixture> CreateAsync()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"conduitai-sqlite-tests-{Guid.NewGuid()}");
            Directory.CreateDirectory(directory);
            var dbPath = Path.Combine(directory, "test.db");
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            var db = new AppDbContext(options);
            await db.Database.MigrateAsync();
            return new SqliteDbFixture(directory, db);
        }

        public void Dispose()
        {
            Db.Dispose();
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
    }
}
