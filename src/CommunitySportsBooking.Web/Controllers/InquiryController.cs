using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using CommunitySportsBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CommunitySportsBooking.Web.Controllers;

// No [Authorize] anywhere in this controller — Inquiry is explicitly a
// Guest-facing feature (no authentication required), though a signed-in
// Member is free to use it too; nothing here reads or requires a Member
// identity.
public class InquiryController : Controller
{
    private readonly AppDbContext _context;

    public InquiryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new InquiryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InquiryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.Inquiries.Add(new Inquiry
        {
            Name = model.Name,
            Email = model.Email,
            Subject = model.Subject,
            Message = model.Message
            // Status defaults to 'New' — Inquiry.Status default, never set by the client.
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you — your inquiry has been submitted. We'll get back to you soon.";
        return RedirectToAction(nameof(Create));
    }
}
