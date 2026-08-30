using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Extensions;
using CommunitySportsBooking.Web.Models.ViewModels;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Controllers;

// Class-level [Authorize]: present from the moment this controller exists,
// not deferred to a later story — mirrors Phase 5's AccountController.Logout.
[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var memberId = User.GetMemberId();
        var member = await _context.Members.SingleAsync(m => m.MemberId == memberId);

        var model = new ProfileViewModel
        {
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Phone = member.Phone,
            AddressLine = member.AddressLine,
            City = member.City,
            RegisteredDate = member.RegisteredDate,
            AvailableSports = await SportsPreferenceService.GetAvailableSportsAsync(_context, memberId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model)
    {
        var memberId = User.GetMemberId();

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildProfileViewModelAsync(memberId, model));
        }

        var member = await _context.Members.SingleAsync(m => m.MemberId == memberId);
        member.FirstName = model.FirstName;
        member.LastName = model.LastName;
        member.Phone = model.Phone;
        member.AddressLine = model.AddressLine;
        member.City = model.City;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sports(SportsPreferencesViewModel model)
    {
        var memberId = User.GetMemberId();
        await SportsPreferenceService.ReconcileAsync(_context, memberId, model.SelectedSportIds);
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel> BuildProfileViewModelAsync(int memberId, ProfileEditViewModel failedEdit)
    {
        var member = await _context.Members.SingleAsync(m => m.MemberId == memberId);
        return new ProfileViewModel
        {
            FirstName = failedEdit.FirstName,
            LastName = failedEdit.LastName,
            Email = member.Email,
            Phone = failedEdit.Phone,
            AddressLine = failedEdit.AddressLine,
            City = failedEdit.City,
            RegisteredDate = member.RegisteredDate,
            AvailableSports = await SportsPreferenceService.GetAvailableSportsAsync(_context, memberId)
        };
    }
}
