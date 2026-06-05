using ConduitAI.Models;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Data;

/// <summary>
/// EF Core database context for ConduitAI. Enums are stored as strings so the
/// SQLite database stays human-readable during demos and interviews.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadInteraction> LeadInteractions => Set<LeadInteraction>();
    public DbSet<LeadAnalysis> LeadAnalyses => Set<LeadAnalysis>();
    public DbSet<MeetingNote> MeetingNotes => Set<MeetingNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.Property(l => l.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(l => l.LeadSource).HasConversion<string>().HasMaxLength(30);
            entity.Property(l => l.Budget).HasColumnType("decimal(18,2)");
            entity.HasIndex(l => l.Status);
            entity.HasIndex(l => l.UpdatedAt);

            entity.HasMany(l => l.Interactions)
                  .WithOne(i => i.Lead)
                  .HasForeignKey(i => i.LeadId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(l => l.Analyses)
                  .WithOne(a => a.Lead)
                  .HasForeignKey(a => a.LeadId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(l => l.MeetingNotes)
                  .WithOne(m => m.Lead)
                  .HasForeignKey(m => m.LeadId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LeadInteraction>(entity =>
        {
            entity.Property(i => i.InteractionType).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(i => new { i.LeadId, i.OccurredAt });
        });

        modelBuilder.Entity<LeadAnalysis>(entity =>
        {
            entity.Property(a => a.UrgencyLevel).HasConversion<string>().HasMaxLength(20);
            entity.Property(a => a.BuyingIntent).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(a => new { a.LeadId, a.GeneratedAt });
        });

        modelBuilder.Entity<MeetingNote>(entity =>
        {
            entity.HasIndex(m => m.CreatedAt);
        });
    }
}
