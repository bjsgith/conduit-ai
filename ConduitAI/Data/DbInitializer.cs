using ConduitAI.Models;
using ConduitAI.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ConduitAI.Data;

/// <summary>
/// Applies migrations and seeds fictional real-estate demo data on first run.
/// Seeding is idempotent: it only runs when the Leads table is empty.
/// </summary>
public static class DbInitializer
{
    private const string SeedModelName = "seed";
    private const string SeedPromptVersion = "seed-v1";

    public static void Initialize(AppDbContext db)
    {
        db.Database.Migrate();

        if (db.Leads.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var leads = new List<Lead>
        {
            new()
            {
                Name = "Marcus Whitfield",
                Email = "marcus.whitfield@example.com",
                Phone = "480-555-0142",
                LeadSource = LeadSource.Referral,
                Budget = 875_000m,
                Location = "Scottsdale, AZ",
                Status = LeadStatus.Qualified,
                Notes = "Relocating from Chicago for a new role. Buyer stated a strong preference for school-district information and wants to be settled before the fall semester.",
                CreatedAt = now.AddDays(-18),
                UpdatedAt = now.AddDays(-1),
                Interactions = new List<LeadInteraction>
                {
                    new() { InteractionType = InteractionType.PhoneCall, OccurredAt = now.AddDays(-18), CreatedAt = now.AddDays(-18), Notes = "Intro call. Confirmed budget up to 900k and a hard relocation deadline of August." },
                    new() { InteractionType = InteractionType.Email, OccurredAt = now.AddDays(-12), CreatedAt = now.AddDays(-12), Notes = "Sent three Scottsdale listings matching the buyer's stated school-district preference." },
                    new() { InteractionType = InteractionType.PropertyTour, OccurredAt = now.AddDays(-3), CreatedAt = now.AddDays(-3), Notes = "Toured two homes near Gainey Ranch. Liked the second; wants a second viewing with spouse." }
                },
                Analyses = new List<LeadAnalysis>
                {
                    new()
                    {
                        Summary = "Relocating buyer with a firm August deadline and a stated school-district preference in Scottsdale. Budget and intent are well qualified.",
                        LeadScore = 88,
                        UrgencyLevel = UrgencyLevel.High,
                        BuyingIntent = BuyingIntent.High,
                        RecommendedNextAction = "Schedule a second viewing of the Gainey Ranch home within 48 hours and prepare comparable listings that match the stated school-district preference.",
                        GeneratedAt = now.AddDays(-3),
                        ModelName = SeedModelName,
                        PromptVersion = SeedPromptVersion
                    }
                }
            },
            new()
            {
                Name = "Priya Nair",
                Email = "priya.nair@example.com",
                Phone = "602-555-0188",
                LeadSource = LeadSource.Zillow,
                Budget = 540_000m,
                Location = "Tempe, AZ",
                Status = LeadStatus.Contacted,
                Notes = "First-time buyer, pre-approval in progress. Interested in walkable neighborhoods near light rail.",
                CreatedAt = now.AddDays(-9),
                UpdatedAt = now.AddDays(-4),
                Interactions = new List<LeadInteraction>
                {
                    new() { InteractionType = InteractionType.Email, OccurredAt = now.AddDays(-9), CreatedAt = now.AddDays(-9), Notes = "Replied to Zillow inquiry; asked about pre-approval status and timeline." },
                    new() { InteractionType = InteractionType.PhoneCall, OccurredAt = now.AddDays(-4), CreatedAt = now.AddDays(-4), Notes = "Discussed light-rail-adjacent condos. Lender pre-approval expected next week." }
                }
            },
            new()
            {
                Name = "Daniel & Rebecca Foss",
                Email = "foss.family@example.com",
                Phone = "480-555-0119",
                LeadSource = LeadSource.OpenHouse,
                Budget = 1_250_000m,
                Location = "Paradise Valley, AZ",
                Status = LeadStatus.TourScheduled,
                Notes = "Move-up buyers selling current Phoenix home. Want a single-story with a casita and mountain views.",
                CreatedAt = now.AddDays(-25),
                UpdatedAt = now.AddDays(-2),
                Interactions = new List<LeadInteraction>
                {
                    new() { InteractionType = InteractionType.Meeting, OccurredAt = now.AddDays(-25), CreatedAt = now.AddDays(-25), Notes = "Met at the Mummy Mountain open house. Strong interest in single-story luxury inventory." },
                    new() { InteractionType = InteractionType.FollowUp, OccurredAt = now.AddDays(-10), CreatedAt = now.AddDays(-10), Notes = "Discussed contingency on selling their current home. Comfortable with a bridge timeline." },
                    new() { InteractionType = InteractionType.PropertyTour, OccurredAt = now.AddDays(-2), CreatedAt = now.AddDays(-2), Notes = "Scheduled private tour of two Paradise Valley estates for this weekend." }
                },
                Analyses = new List<LeadAnalysis>
                {
                    new()
                    {
                        Summary = "High-budget move-up buyers with a sale contingency. Clear preferences (single-story, casita, views) and an active tour scheduled.",
                        LeadScore = 79,
                        UrgencyLevel = UrgencyLevel.High,
                        BuyingIntent = BuyingIntent.Medium,
                        RecommendedNextAction = "Confirm weekend tour logistics and bring a market analysis for their current home to address the sale contingency.",
                        GeneratedAt = now.AddDays(-2),
                        ModelName = SeedModelName,
                        PromptVersion = SeedPromptVersion
                    }
                }
            },
            new()
            {
                Name = "Sofia Alvarez",
                Email = "sofia.alvarez@example.com",
                Phone = "623-555-0173",
                LeadSource = LeadSource.Website,
                Budget = 410_000m,
                Location = "Gilbert, AZ",
                Status = LeadStatus.New,
                Notes = "Submitted a contact form about new-build communities. No timeline shared yet.",
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            },
            new()
            {
                Name = "James Okafor",
                Email = "james.okafor@example.com",
                Phone = "480-555-0155",
                LeadSource = LeadSource.SocialMedia,
                Budget = 690_000m,
                Location = "Chandler, AZ",
                Status = LeadStatus.OfferPending,
                Notes = "Investor evaluating a rental property. Offer submitted; awaiting seller response.",
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddHours(-20),
                Interactions = new List<LeadInteraction>
                {
                    new() { InteractionType = InteractionType.PhoneCall, OccurredAt = now.AddDays(-30), CreatedAt = now.AddDays(-30), Notes = "Discussed cash-flow targets and preferred Chandler rental corridors." },
                    new() { InteractionType = InteractionType.Note, OccurredAt = now.AddDays(-6), CreatedAt = now.AddDays(-6), Notes = "Ran rent comps; property pencils at roughly 6.5% cap rate." },
                    new() { InteractionType = InteractionType.FollowUp, OccurredAt = now.AddHours(-20), CreatedAt = now.AddHours(-20), Notes = "Submitted offer at asking with a 21-day close. Awaiting seller counter." }
                }
            },
            new()
            {
                Name = "Hannah Berg",
                Email = "hannah.berg@example.com",
                Phone = "602-555-0107",
                LeadSource = LeadSource.RealtorCom,
                Budget = 365_000m,
                Location = "Mesa, AZ",
                Status = LeadStatus.Lost,
                Notes = "Paused search; decided to renew their lease for another year.",
                CreatedAt = now.AddDays(-45),
                UpdatedAt = now.AddDays(-14),
                Interactions = new List<LeadInteraction>
                {
                    new() { InteractionType = InteractionType.Email, OccurredAt = now.AddDays(-45), CreatedAt = now.AddDays(-45), Notes = "Initial outreach about Mesa starter homes." },
                    new() { InteractionType = InteractionType.PhoneCall, OccurredAt = now.AddDays(-14), CreatedAt = now.AddDays(-14), Notes = "Decided to pause and renew lease. Re-engage in ~10 months." }
                }
            },
            new()
            {
                Name = "Thomas Reed",
                Email = "thomas.reed@example.com",
                Phone = "480-555-0166",
                LeadSource = LeadSource.Referral,
                Budget = 760_000m,
                Location = "Scottsdale, AZ",
                Status = LeadStatus.Closed,
                Notes = "Closed on a North Scottsdale property last month. Great referral source going forward.",
                CreatedAt = now.AddDays(-90),
                UpdatedAt = now.AddDays(-30),
                Interactions = new List<LeadInteraction>
                {
                    new() { InteractionType = InteractionType.Meeting, OccurredAt = now.AddDays(-90), CreatedAt = now.AddDays(-90), Notes = "Referred by Marcus Whitfield. Buyer consultation completed." },
                    new() { InteractionType = InteractionType.Note, OccurredAt = now.AddDays(-30), CreatedAt = now.AddDays(-30), Notes = "Closed escrow. Sent closing gift and requested a testimonial." }
                }
            },
            new()
            {
                Name = "Grace Lin",
                Email = "grace.lin@example.com",
                Phone = "623-555-0190",
                LeadSource = LeadSource.OpenHouse,
                Budget = 525_000m,
                Location = "Gilbert, AZ",
                Status = LeadStatus.Contacted,
                Notes = "Comparing Gilbert and Chandler. Wants a low-maintenance yard and a home office.",
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-3),
                Interactions = new List<LeadInteraction>
                {
                    new() { InteractionType = InteractionType.Meeting, OccurredAt = now.AddDays(-5), CreatedAt = now.AddDays(-5), Notes = "Met at the Gilbert open house. Collected must-haves: office, low-maintenance yard." },
                    new() { InteractionType = InteractionType.Email, OccurredAt = now.AddDays(-3), CreatedAt = now.AddDays(-3), Notes = "Sent a side-by-side of three Gilbert and two Chandler listings." }
                }
            }
        };

        db.Leads.AddRange(leads);
        db.SaveChanges();
    }
}
