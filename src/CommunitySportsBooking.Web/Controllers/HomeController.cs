using System.Diagnostics;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models;
using CommunitySportsBooking.Web.Models.ViewModels;
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
        var activeFacilities = await _context.Facilities
            .Where(f => f.IsActive)
            .OrderBy(f => f.FacilityName)
            .ToListAsync();

        var model = new HomeIndexViewModel
        {
            ActiveFacilityCount = activeFacilities.Count,
            FeaturedFacilityNames = activeFacilities.Take(3).Select(f => f.FacilityName).ToList()
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
