using System.Diagnostics;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models;
using CommunitySportsBooking.Web.Models.ViewModels;
using CommunitySportsBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySportsBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var activeFacilityCount = await _context.Facilities.CountAsync(f => f.IsActive);

        var featured = await _context.Facilities
            .Where(f => f.IsActive)
            .OrderBy(f => f.FacilityName)
            .Take(3)
            .Select(FacilityProjections.ToSummary())
            .ToListAsync();

        var popularSports = await _context.Sports
            .OrderBy(s => s.SportName)
            .Select(s => new SportOptionViewModel { SportId = s.SportId, SportName = s.SportName })
            .ToListAsync();

        var model = new HomeIndexViewModel
        {
            ActiveFacilityCount = activeFacilityCount,
            FeaturedFacilities = featured,
            PopularSports = popularSports
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Backing action for app.UseStatusCodePagesWithReExecute (Program.cs) —
    // gives 404/403/etc. a branded page instead of a bare status response.
    // Named StatusCodePage (with [ActionName] keeping the /Home/StatusCode
    // URL) rather than StatusCode, since ControllerBase already defines a
    // same-named helper method with different semantics (returns a bare
    // StatusCodeResult) — reusing that name would hide it, not override it.
    //
    // Parameter MUST be named "id", not "code": the app's conventional route
    // is {controller}/{action}/{id?}, so the URL's third segment (the actual
    // status code, e.g. "404") is bound to route-data key "id". A parameter
    // named "code" never matches that key, silently binds to 0, and Response
    // .StatusCode = 0 produces a malformed HTTP status line ("HTTP/1.1 0")
    // that real HTTP clients reject outright — caught by an actual raw-socket
    // request, not by dotnet build (Razor/C# compiled fine either way). Same
    // route-parameter-name class of bug as FacilityController.Details.
    [ActionName("StatusCode")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int id)
    {
        Response.StatusCode = id;
        return View("StatusCode", id);
    }
}
