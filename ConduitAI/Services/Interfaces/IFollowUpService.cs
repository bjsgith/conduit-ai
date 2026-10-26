using ConduitAI.ViewModels;

namespace ConduitAI.Services.Interfaces;

public interface IFollowUpService
{
    Task<IReadOnlyList<LeadFollowUpViewModel>> GetForLeadAsync(int leadId);
    Task<int?> CreateAsync(FollowUpFormViewModel form);
    Task<FollowUpFormViewModel?> GetForEditAsync(int id);
    Task<bool> RescheduleAsync(FollowUpFormViewModel form);
    Task<int?> CompleteAsync(int id);
    Task<int?> DeleteAsync(int id);
}
