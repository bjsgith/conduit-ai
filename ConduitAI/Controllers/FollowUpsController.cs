using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ConduitAI.Controllers;

public class FollowUpsController : Controller
{
    private readonly IFollowUpService _followUps;
    private readonly ILeadService _leads;

    public FollowUpsController(IFollowUpService followUps, ILeadService leads)
    {
        _followUps = followUps;
        _leads = leads;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int leadId, string? actionText)
    {
        var lead = await _leads.GetByIdAsync(leadId);
        if (lead is null)
        {
            return NotFound();
        }

        return View("Form", new FollowUpFormViewModel
        {
            LeadId = lead.Id,
            LeadName = lead.Name,
            ActionText = actionText ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpFormViewModel form)
    {
        var lead = await _leads.GetByIdAsync(form.LeadId);
        if (lead is null)
        {
            return NotFound();
        }

        form.LeadName = lead.Name;
        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        var id = await _followUps.CreateAsync(form);
        if (!id.HasValue)
        {
            return NotFound();
        }

        TempData["Flash"] = "Follow-up scheduled.";
        return RedirectToAction("Details", "Leads", new { id = form.LeadId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _followUps.GetForEditAsync(id);
        return form is null ? NotFound() : View("Form", form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUpFormViewModel form)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        var lead = await _leads.GetByIdAsync(form.LeadId);
        if (lead is null)
        {
            return NotFound();
        }

        form.LeadName = lead.Name;
        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        var updated = await _followUps.RescheduleAsync(form);
        if (!updated)
        {
            return NotFound();
        }

        TempData["Flash"] = "Follow-up updated.";
        return RedirectToAction("Details", "Leads", new { id = form.LeadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var leadId = await _followUps.CompleteAsync(id);
        if (!leadId.HasValue)
        {
            return NotFound();
        }

        TempData["Flash"] = "Follow-up marked complete.";
        return RedirectToAction("Details", "Leads", new { id = leadId.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var leadId = await _followUps.DeleteAsync(id);
        if (!leadId.HasValue)
        {
            return NotFound();
        }

        TempData["Flash"] = "Follow-up removed.";
        return RedirectToAction("Details", "Leads", new { id = leadId.Value });
    }
}
