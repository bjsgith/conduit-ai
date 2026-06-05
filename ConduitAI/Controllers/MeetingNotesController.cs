using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConduitAI.Controllers;

public class MeetingNotesController : Controller
{
    private readonly IMeetingNotesService _meetingNotes;
    private readonly ILeadService _leads;

    public MeetingNotesController(IMeetingNotesService meetingNotes, ILeadService leads)
    {
        _meetingNotes = meetingNotes;
        _leads = leads;
    }

    // GET /MeetingNotes/Create?leadId=5
    public async Task<IActionResult> Create(int? leadId)
    {
        var model = new MeetingNotesFormViewModel
        {
            LeadId = leadId,
            LeadOptions = await BuildLeadOptionsAsync()
        };
        return View(model);
    }

    // POST /MeetingNotes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MeetingNotesFormViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            form.LeadOptions = await BuildLeadOptionsAsync();
            return View(form);
        }

        var result = await _meetingNotes.CreateAsync(form, ct);
        if (!result.Success || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Meeting notes could not be analyzed.");
            form.LeadOptions = await BuildLeadOptionsAsync();
            return View(form);
        }

        if (result.Value.LeadId.HasValue)
        {
            TempData["Flash"] = "Meeting notes analyzed and attached to the lead.";
            return RedirectToAction("Details", "Leads", new { id = result.Value.LeadId.Value });
        }

        return View("Result", new MeetingNotesResultViewModel { Note = result.Value });
    }

    private async Task<IEnumerable<SelectListItem>> BuildLeadOptionsAsync()
    {
        var options = await _leads.GetLeadOptionsAsync();
        return options
            .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
            .ToList();
    }
}
