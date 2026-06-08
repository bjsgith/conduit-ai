using ConduitAI.Services.Ai;
using ConduitAI.ViewModels;

namespace ConduitAI.Services.Interfaces;

public interface IMeetingNotesService
{
    Task<AiOperationResult<MeetingNoteSummaryViewModel>> CreateAsync(MeetingNotesFormViewModel form, CancellationToken ct = default);
    Task<IReadOnlyList<MeetingNoteSummaryViewModel>> GetForLeadAsync(int leadId);
    Task<MeetingNoteSummaryViewModel?> GetByIdAsync(int id);
}
