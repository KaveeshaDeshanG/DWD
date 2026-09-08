using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Areas.Admin.Controllers;

public class InquiriesController : AdminControllerBase
{
    // Mirrors CK_Inquiry_Status (database/02_CreateTables.sql) exactly — the
    // only three values the database itself will ever accept.
    private static readonly string[] ValidStatuses = { "New", "Reviewed", "Closed" };

    private readonly AppDbContext _context;

    public InquiriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = _context.Inquiries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                i.Name.Contains(search) ||
                i.Email.Contains(search) ||
                i.Subject.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        var inquiries = await query
            .OrderByDescending(i => i.InquiryDate)
            .Select(i => new InquiryListItemViewModel
            {
                InquiryId = i.InquiryId,
                Name = i.Name,
                Email = i.Email,
                Subject = i.Subject,
                InquiryDate = i.InquiryDate,
                Status = i.Status
            })
            .ToListAsync();

        return View(new InquiryIndexViewModel { Search = search, Status = status, Inquiries = inquiries });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var inquiry = await _context.Inquiries.SingleOrDefaultAsync(i => i.InquiryId == id);
        if (inquiry is null)
        {
            return NotFound();
        }

        return View(new InquiryDetailsViewModel
        {
            InquiryId = inquiry.InquiryId,
            Name = inquiry.Name,
            Email = inquiry.Email,
            Subject = inquiry.Subject,
            Message = inquiry.Message,
            InquiryDate = inquiry.InquiryDate,
            Status = inquiry.Status
        });
    }

    // Only the three CK_Inquiry_Status values are ever accepted — anything
    // else is rejected here, before it can reach the database's own CHECK
    // constraint as an unhandled exception.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var inquiry = await _context.Inquiries.SingleOrDefaultAsync(i => i.InquiryId == id);
        if (inquiry is null)
        {
            return NotFound();
        }

        if (!ValidStatuses.Contains(status))
        {
            TempData["ErrorMessage"] = "Invalid status.";
            return RedirectToAction(nameof(Details), new { id });
        }

        inquiry.Status = status;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Inquiry status updated.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
