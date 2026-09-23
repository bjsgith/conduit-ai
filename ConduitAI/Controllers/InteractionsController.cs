using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ConduitAI.Controllers;

public class InteractionsController : Controller
{
    private readonly ITimelineService _timeline;
    private readonly ILeadService _leads;
    private readonly IFollowUpService _followUps;

    public InteractionsController(ITimelineService timeline, ILeadService leads, IFollowUpService followUps)
    {
        _timeline = timeline;
        _leads = leads;
        _followUps = followUps;
    }

    // POST /Interactions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewInteraction")] InteractionFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayLeadAsync(form);
        }

        var added = await _timeline.AddInteractionAsync(form);
        if (!added)
        {
            return NotFound();
        }

        TempData["Flash"] = "Interaction added to the timeline.";
        return RedirectToAction("Details", "Leads", new { id = form.LeadId });
    }

    private async Task<IActionResult> RedisplayLeadAsync(InteractionFormViewModel form)
    {
        var details = await _leads.GetDetailsAsync(form.LeadId);
        if (details is null)
        {
            return NotFound();
        }

        details.NewInteraction = form;
        details.FollowUps = await _followUps.GetForLeadAsync(form.LeadId);
        ViewData["Title"] = details.Lead.Name;
        return View("~/Views/Leads/Details.cshtml", details);
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
