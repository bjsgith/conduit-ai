using ConduitAI.Services.Ai;
using ConduitAI.ViewModels;

namespace ConduitAI.Services.Interfaces;

public interface IAiAnalysisService
{
    Task<AiAnalysisViewModel?> GetLatestAsync(int leadId);
    Task<AiOperationResult<AiAnalysisViewModel>> GenerateAsync(int leadId, CancellationToken ct = default);
}
