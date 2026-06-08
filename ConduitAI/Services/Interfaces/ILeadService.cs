using ConduitAI.Models;
using ConduitAI.ViewModels;

namespace ConduitAI.Services.Interfaces;

public interface ILeadService
{
    Task<LeadListViewModel> GetLeadListAsync(LeadFilterViewModel filter);
    Task<LeadDetailsViewModel?> GetDetailsAsync(int id);
    Task<Lead?> GetByIdAsync(int id);
    Task<LeadFormViewModel?> GetFormAsync(int id);
    Task<int> CreateAsync(LeadFormViewModel form);
    Task<bool> UpdateAsync(LeadFormViewModel form);
    Task<bool> DeleteAsync(int id);
    Task TouchAsync(int leadId);
    Task<IReadOnlyList<(int Id, string Name)>> GetLeadOptionsAsync();
}
