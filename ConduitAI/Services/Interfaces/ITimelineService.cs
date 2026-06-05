using ConduitAI.Models;
using ConduitAI.ViewModels;

namespace ConduitAI.Services.Interfaces;

public interface ITimelineService
{
    Task<IReadOnlyList<LeadInteraction>> GetForLeadAsync(int leadId);
    Task<bool> AddInteractionAsync(InteractionFormViewModel form);
    Task<int?> DeleteInteractionAsync(int interactionId);
}
