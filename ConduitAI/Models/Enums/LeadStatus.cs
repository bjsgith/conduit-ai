namespace ConduitAI.Models.Enums;

/// <summary>
/// Lifecycle stage of a lead as it moves through the sales pipeline.
/// </summary>
public enum LeadStatus
{
    New,
    Contacted,
    Qualified,
    TourScheduled,
    OfferPending,
    Closed,
    Lost
}
