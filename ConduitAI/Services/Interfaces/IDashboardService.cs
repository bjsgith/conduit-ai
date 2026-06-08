using ConduitAI.ViewModels;

namespace ConduitAI.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync();
}
