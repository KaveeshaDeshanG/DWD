using System.ComponentModel.DataAnnotations;
using CommunitySportsBooking.Web.Areas.Admin.Controllers;
using CommunitySportsBooking.Web.Areas.Admin.ViewModels;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Admin Panel, Phase 4: Sport Management. Same discipline as every other
// test class here — real CommunitySportsBookingDB, no mocking. Read-only/
// referenced-sport assertions anchor on seeded Sport 1 (Tennis — referenced
// by FacilitySport via Riverside Tennis Courts and MemberSport via Alice/
// Emma), never mutated by these tests. Create/Edit/safe-Delete tests use
// their own disposable throwaway sport with a GUID-suffixed name so they can
// never collide with the real 8-sport catalog.
[Collection("Database collection")]
public class AdminSportManagementTests
{
    private const string ConnectionString =
        "Server=localhost;Database=CommunitySportsBookingDB;Trusted_Connection=True;TrustServerCertificate=True;";

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static string NewThrowawaySportName() => $"TestSport{Guid.NewGuid():N}"[..20];

    private static async Task DeleteSportIfExistsAsync(int sportId)
    {
        await using var context = CreateContext();
        var row = await context.Sports.FindAsync(sportId);
        if (row is not null)
        {
            context.Sports.Remove(row);
            await context.SaveChangesAsync();
        }
    }

    // Same no-op ITempDataProvider as every other admin test class — Create/
    // Edit/Delete write TempData success/error messages.
    private sealed class NoOpTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static SportsController CreateSportsController(AppDbContext context)
    {
        var httpContext = new DefaultHttpContext();
        return new SportsController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NoOpTempDataProvider())
        };
    }

    // Mirrors AccountFunctionalityTests.ApplyRealValidation — exercises the
    // real [Required]/[StringLength] attributes rather than hand-adding a
    // ModelState error, the same discipline the rest of this project uses
    // for validation tests.
    private static void ApplyRealValidation(ControllerBase controller, object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        foreach (var result in results)
        {
            foreach (var memberName in result.MemberNames.DefaultIfEmpty(string.Empty))
            {
                controller.ModelState.AddModelError(memberName, result.ErrorMessage ?? "Invalid");
            }
        }
    }

    // ---------- Index ----------

    [Fact]
    public async Task Index_NoSearch_ReturnsAllSportsFromDatabase()
    {
        await using var context = CreateContext();
        var controller = CreateSportsController(context);

        var result = await controller.Index(search: null);

        var model = Assert.IsType<SportIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        await using var verify = CreateContext();
        Assert.Equal(await verify.Sports.CountAsync(), model.Sports.Count);
    }

    [Fact]
    public async Task Index_SearchByName_ReturnsOnlyMatchingSports()
    {
        await using var context = CreateContext();
        var controller = CreateSportsController(context);

        var result = await controller.Index(search: "Tennis");

        var model = Assert.IsType<SportIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Single(model.Sports);
        Assert.Equal("Tennis", model.Sports[0].SportName);
    }

    // ---------- Details ----------

    [Fact]
    public async Task Details_ReferencedSport_MatchesIndependentCounts_AndCannotBeDeleted()
    {
        await using var context = CreateContext();
        var controller = CreateSportsController(context);

        var result = await controller.Details(id: 1); // Tennis (seed)

        var model = Assert.IsType<SportDetailsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Tennis", model.SportName);

        await using var verify = CreateContext();
        Assert.Equal(await verify.FacilitySports.CountAsync(fs => fs.SportId == 1), model.Facilities.Count);
        Assert.Equal(await verify.MemberSports.CountAsync(ms => ms.SportId == 1), model.MemberPreferenceCount);
        Assert.NotEmpty(model.Facilities);
        Assert.False(model.CanDelete);
    }

    [Fact]
    public async Task Details_NonexistentSport_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateSportsController(context);

        var result = await controller.Details(999999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidModel_PersistsSportAndRedirects()
    {
        var name = NewThrowawaySportName();
        await using var context = CreateContext();
        var controller = CreateSportsController(context);

        int? createdId = null;
        try
        {
            var result = await controller.Create(new SportFormViewModel { SportName = name });

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            var created = await verify.Sports.SingleOrDefaultAsync(s => s.SportName == name);
            Assert.NotNull(created);
            createdId = created!.SportId;
        }
        finally
        {
            if (createdId is not null)
            {
                await DeleteSportIfExistsAsync(createdId.Value);
            }
        }
    }

    [Fact]
    public async Task Create_DuplicateName_RejectedWithoutCreatingSecondRow()
    {
        await using var context = CreateContext();
        var controller = CreateSportsController(context);
        var beforeCount = await context.Sports.CountAsync();

        var result = await controller.Create(new SportFormViewModel { SportName = "Tennis" });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        await using var verify = CreateContext();
        Assert.Equal(beforeCount, await verify.Sports.CountAsync());
    }

    [Fact]
    public async Task Create_EmptyName_RealValidationRejectsIt_NoRowCreated()
    {
        await using var context = CreateContext();
        var controller = CreateSportsController(context);
        var beforeCount = await context.Sports.CountAsync();
        var model = new SportFormViewModel { SportName = string.Empty };
        ApplyRealValidation(controller, model); // exercises the real [Required] attribute
        Assert.False(controller.ModelState.IsValid);

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        await using var verify = CreateContext();
        Assert.Equal(beforeCount, await verify.Sports.CountAsync());
    }

    // ---------- Edit ----------

    [Fact]
    public async Task Edit_ValidModel_UpdatesNameAndPersists()
    {
        var originalName = NewThrowawaySportName();
        await using var setupContext = CreateContext();
        var sport = new Sport { SportName = originalName };
        setupContext.Sports.Add(sport);
        await setupContext.SaveChangesAsync();
        var sportId = sport.SportId;

        try
        {
            var newName = NewThrowawaySportName();
            await using var context = CreateContext();
            var controller = CreateSportsController(context);

            var result = await controller.Edit(sportId, new SportFormViewModel { SportName = newName });

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            var reloaded = await verify.Sports.FindAsync(sportId);
            Assert.Equal(newName, reloaded!.SportName);
        }
        finally
        {
            await DeleteSportIfExistsAsync(sportId);
        }
    }

    [Fact]
    public async Task Edit_DuplicateName_Rejected_NameUnchanged()
    {
        var originalName = NewThrowawaySportName();
        await using var setupContext = CreateContext();
        var sport = new Sport { SportName = originalName };
        setupContext.Sports.Add(sport);
        await setupContext.SaveChangesAsync();
        var sportId = sport.SportId;

        try
        {
            await using var context = CreateContext();
            var controller = CreateSportsController(context);

            // "Tennis" already exists (seed) — editing this throwaway sport to
            // that name must be rejected, same as Create's duplicate check.
            var result = await controller.Edit(sportId, new SportFormViewModel { SportName = "Tennis" });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            await using var verify = CreateContext();
            var reloaded = await verify.Sports.FindAsync(sportId);
            Assert.Equal(originalName, reloaded!.SportName);
        }
        finally
        {
            await DeleteSportIfExistsAsync(sportId);
        }
    }

    [Fact]
    public async Task Edit_NonexistentSport_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateSportsController(context);

        var result = await controller.Edit(999999, new SportFormViewModel { SportName = "Doesn't Matter" });

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_ReferencedSport_IsRejected_DatabaseUnchanged()
    {
        await using var context = CreateContext();
        var controller = CreateSportsController(context);

        var result = await controller.Delete(1); // Tennis — referenced by FacilitySport + MemberSport

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Contains("Cannot delete", (string)controller.TempData["ErrorMessage"]!);

        await using var verify = CreateContext();
        Assert.NotNull(await verify.Sports.FindAsync(1));
    }

    [Fact]
    public async Task Delete_UnreferencedSport_Succeeds()
    {
        var name = NewThrowawaySportName();
        await using var setupContext = CreateContext();
        var sport = new Sport { SportName = name };
        setupContext.Sports.Add(sport);
        await setupContext.SaveChangesAsync();
        var sportId = sport.SportId;

        try
        {
            await using var context = CreateContext();
            var controller = CreateSportsController(context);

            var result = await controller.Delete(sportId);

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            Assert.Null(await verify.Sports.FindAsync(sportId));
        }
        finally
        {
            // Already deleted by the action under test in the success path;
            // this is a no-op safety net in case the assertion above failed.
            await DeleteSportIfExistsAsync(sportId);
        }
    }

    [Fact]
    public async Task Delete_NonexistentSport_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateSportsController(context);

        var result = await controller.Delete(999999);

        Assert.IsType<NotFoundResult>(result);
    }
}
