using ConduitAI.Services.Interfaces;
using ConduitAI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ConduitAI.Controllers;

public class LeadsController : Controller
{
    private readonly ILeadService _leads;

    public LeadsController(ILeadService leads)
    {
        _leads = leads;
    }

    // GET /Leads
    public async Task<IActionResult> Index([FromQuery] LeadFilterViewModel filter)
    {
        var model = await _leads.GetLeadListAsync(filter);
        return View(model);
    }

    // GET /Leads/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var model = await _leads.GetDetailsAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    // GET /Leads/Create
    public IActionResult Create()
    {
        return View(new LeadFormViewModel());
    }

    // POST /Leads/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var id = await _leads.CreateAsync(form);
        TempData["Flash"] = "Lead created.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET /Leads/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _leads.GetFormAsync(id);
        if (form is null)
        {
            return NotFound();
        }

        return View(form);
    }

    // POST /Leads/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadFormViewModel form)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var updated = await _leads.UpdateAsync(form);
        if (!updated)
        {
            return NotFound();
        }

        TempData["Flash"] = "Lead updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET /Leads/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _leads.GetByIdAsync(id);
        if (lead is null)
        {
            return NotFound();
        }

        return View(lead);
    }

    // POST /Leads/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deleted = await _leads.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["Flash"] = "Lead deleted.";
        return RedirectToAction(nameof(Index));
    }
}
