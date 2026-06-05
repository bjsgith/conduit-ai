using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ConduitAI.Controllers;

public class InteractionsController : Controller
{
    private readonly ITimelineService _timeline;

    public InteractionsController(ITimelineService timeline)
    {
        _timeline = timeline;
    }

    // POST /Interactions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InteractionFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["FlashError"] = "Could not add the interaction. Please check the form and try again.";
            return RedirectToAction("Details", "Leads", new { id = form.LeadId });
        }

        var added = await _timeline.AddInteractionAsync(form);
        if (!added)
        {
            return NotFound();
        }

        TempData["Flash"] = "Interaction added to the timeline.";
        return RedirectToAction("Details", "Leads", new { id = form.LeadId });
    }

    // POST /Interactions/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var leadId = await _timeline.DeleteInteractionAsync(id);
        if (leadId is null)
        {
            return NotFound();
        }

        TempData["Flash"] = "Interaction removed.";
        return RedirectToAction("Details", "Leads", new { id = leadId.Value });
    }
}
