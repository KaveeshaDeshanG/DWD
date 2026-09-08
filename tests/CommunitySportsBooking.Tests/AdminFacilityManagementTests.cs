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

// Admin Panel, Phase 5: Facility Management. Same discipline as every other
// test class here — real CommunitySportsBookingDB, no mocking. Read-only/
// blocked-deletion assertions anchor on seeded Facility 1 (Riverside Tennis
// Courts — has real bookings and one FacilitySport row), never mutated.
// Create/Edit/Sports-assignment/safe-Delete tests use their own disposable
// throwaway facility with a GUID-suffixed name.
[Collection("Database collection")]
public class AdminFacilityManagementTests
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

    private static string NewThrowawayFacilityName() => $"TestFacility{Guid.NewGuid():N}"[..24];

    private static async Task<int> CreateThrowawayFacilityAsync(bool isActive = true)
    {
        await using var context = CreateContext();
        var facility = new Facility
        {
            FacilityName = NewThrowawayFacilityName(),
            FacilityType = "Test Type",
            Location = "Test Location",
            AddressLine = "1 Test Street",
            City = "Springfield",
            IsActive = isActive
        };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();
        return facility.FacilityId;
    }

    private static async Task DeleteFacilityIfExistsAsync(int facilityId)
    {
        await using var context = CreateContext();
        var row = await context.Facilities.FindAsync(facilityId);
        if (row is not null)
        {
            context.Facilities.Remove(row);
            await context.SaveChangesAsync();
        }
    }

    private sealed class NoOpTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static FacilitiesController CreateFacilitiesController(AppDbContext context)
    {
        var httpContext = new DefaultHttpContext();
        return new FacilitiesController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NoOpTempDataProvider())
        };
    }

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

    private static FacilityFormViewModel ValidForm(string name) => new()
    {
        FacilityName = name,
        FacilityType = "Test Type",
        Location = "Test Location",
        AddressLine = "1 Test Street",
        City = "Springfield"
    };

    // ---------- Index ----------

    [Fact]
    public async Task Index_NoFilters_ReturnsAllFacilitiesFromDatabase()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Index(search: null, status: null);

        var model = Assert.IsType<FacilityIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        await using var verify = CreateContext();
        Assert.Equal(await verify.Facilities.CountAsync(), model.Facilities.Count);
    }

    [Fact]
    public async Task Index_SearchByName_ReturnsOnlyMatchingFacilities()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Index(search: "Riverside", status: null);

        var model = Assert.IsType<FacilityIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Single(model.Facilities);
        Assert.Equal("Riverside Tennis Courts", model.Facilities[0].FacilityName);
    }

    [Fact]
    public async Task Index_StatusActiveFilter_ExcludesTheKnownInactiveSeedFacility()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Index(search: null, status: "active");

        var model = Assert.IsType<FacilityIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.All(model.Facilities, f => Assert.True(f.IsActive));
        Assert.DoesNotContain(model.Facilities, f => f.FacilityName == "Old Mill Badminton Courts"); // seeded inactive
    }

    [Fact]
    public async Task Index_StatusInactiveFilter_ReturnsOnlyInactiveFacilities()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Index(search: null, status: "inactive");

        var model = Assert.IsType<FacilityIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Facilities);
        Assert.All(model.Facilities, f => Assert.False(f.IsActive));
    }

    // ---------- Details ----------

    [Fact]
    public async Task Details_ExistingFacility_MatchesIndependentBookingAndSportCounts()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Details(id: 1); // Riverside Tennis Courts (seed)

        var model = Assert.IsType<FacilityDetailsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Riverside Tennis Courts", model.FacilityName);

        await using var verify = CreateContext();
        Assert.Equal(await verify.Bookings.CountAsync(b => b.FacilityId == 1), model.BookingCount);
        Assert.True(model.BookingCount > 0);
        Assert.False(model.CanDelete);
        Assert.Contains(model.SportOptions, s => s.SportName == "Tennis" && s.IsSelected);
    }

    [Fact]
    public async Task Details_NonexistentFacility_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Details(999999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidModel_PersistsFacilityAsActive()
    {
        var name = NewThrowawayFacilityName();
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        int? createdId = null;
        try
        {
            var result = await controller.Create(ValidForm(name));

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            var created = await verify.Facilities.SingleOrDefaultAsync(f => f.FacilityName == name);
            Assert.NotNull(created);
            Assert.True(created!.IsActive);
            createdId = created.FacilityId;
        }
        finally
        {
            if (createdId is not null)
            {
                await DeleteFacilityIfExistsAsync(createdId.Value);
            }
        }
    }

    [Fact]
    public async Task Create_EmptyName_RealValidationRejectsIt_NoRowCreated()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);
        var beforeCount = await context.Facilities.CountAsync();
        var model = ValidForm(string.Empty);
        ApplyRealValidation(controller, model);
        Assert.False(controller.ModelState.IsValid);

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        await using var verify = CreateContext();
        Assert.Equal(beforeCount, await verify.Facilities.CountAsync());
    }

    // ---------- Edit ----------

    [Fact]
    public async Task Edit_ValidModel_UpdatesFieldsAndPersists()
    {
        var facilityId = await CreateThrowawayFacilityAsync();
        try
        {
            var newName = NewThrowawayFacilityName();
            await using var context = CreateContext();
            var controller = CreateFacilitiesController(context);
            var model = ValidForm(newName);
            model.Capacity = 42;

            var result = await controller.Edit(facilityId, model);

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            var reloaded = await verify.Facilities.FindAsync(facilityId);
            Assert.Equal(newName, reloaded!.FacilityName);
            Assert.Equal(42, reloaded.Capacity);
        }
        finally
        {
            await DeleteFacilityIfExistsAsync(facilityId);
        }
    }

    [Fact]
    public async Task Edit_NonexistentFacility_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Edit(999999, ValidForm("Doesn't Matter"));

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- ToggleActive ----------

    [Fact]
    public async Task ToggleActive_FlipsStatusAndPersists()
    {
        var facilityId = await CreateThrowawayFacilityAsync(isActive: true);
        try
        {
            await using var context = CreateContext();
            var controller = CreateFacilitiesController(context);

            var result = await controller.ToggleActive(facilityId);

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            Assert.False((await verify.Facilities.FindAsync(facilityId))!.IsActive);
        }
        finally
        {
            await DeleteFacilityIfExistsAsync(facilityId);
        }
    }

    [Fact]
    public async Task ToggleActive_NonexistentFacility_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.ToggleActive(999999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- Facility-Sport assignment ----------

    [Fact]
    public async Task Sports_SelectingSports_CreatesFacilitySportRows()
    {
        var facilityId = await CreateThrowawayFacilityAsync();
        try
        {
            await using var context = CreateContext();
            var controller = CreateFacilitiesController(context);

            var result = await controller.Sports(facilityId, new List<int> { 1, 2 }); // Tennis, Basketball

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            var sportIds = await verify.FacilitySports.Where(fs => fs.FacilityId == facilityId).Select(fs => fs.SportId).ToListAsync();
            Assert.Equal(new[] { 1, 2 }, sportIds.OrderBy(id => id));
        }
        finally
        {
            await DeleteFacilityIfExistsAsync(facilityId);
        }
    }

    [Fact]
    public async Task Sports_DeselectingASport_RemovesOnlyThatFacilitySportRow()
    {
        var facilityId = await CreateThrowawayFacilityAsync();
        try
        {
            await using var setupContext = CreateContext();
            var controller1 = CreateFacilitiesController(setupContext);
            await controller1.Sports(facilityId, new List<int> { 1, 2 }); // start with both

            await using var context = CreateContext();
            var controller2 = CreateFacilitiesController(context);
            var result = await controller2.Sports(facilityId, new List<int> { 1 }); // keep only Tennis

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            var sportIds = await verify.FacilitySports.Where(fs => fs.FacilityId == facilityId).Select(fs => fs.SportId).ToListAsync();
            Assert.Equal(new[] { 1 }, sportIds);
        }
        finally
        {
            await DeleteFacilityIfExistsAsync(facilityId);
        }
    }

    [Fact]
    public async Task Sports_InvalidSportId_IsSilentlyIgnored_NoRowCreated()
    {
        var facilityId = await CreateThrowawayFacilityAsync();
        try
        {
            await using var context = CreateContext();
            var controller = CreateFacilitiesController(context);

            var result = await controller.Sports(facilityId, new List<int> { 999999 });

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            Assert.Empty(await verify.FacilitySports.Where(fs => fs.FacilityId == facilityId).ToListAsync());
        }
        finally
        {
            await DeleteFacilityIfExistsAsync(facilityId);
        }
    }

    [Fact]
    public async Task Sports_ReconcilingFacilitySports_NeverTouchesMemberSport()
    {
        var facilityId = await CreateThrowawayFacilityAsync();
        try
        {
            await using var context = CreateContext();
            var beforeMemberSportCount = await context.MemberSports.CountAsync();
            var controller = CreateFacilitiesController(context);

            await controller.Sports(facilityId, new List<int> { 1, 3 });

            await using var verify = CreateContext();
            Assert.Equal(beforeMemberSportCount, await verify.MemberSports.CountAsync());
        }
        finally
        {
            await DeleteFacilityIfExistsAsync(facilityId);
        }
    }

    [Fact]
    public async Task Sports_NonexistentFacility_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Sports(999999, new List<int> { 1 });

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_FacilityWithBookings_IsRejected_DatabaseUnchanged()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Delete(1); // Riverside Tennis Courts — has real bookings

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Contains("Cannot delete", (string)controller.TempData["ErrorMessage"]!);

        await using var verify = CreateContext();
        Assert.NotNull(await verify.Facilities.FindAsync(1));
    }

    [Fact]
    public async Task Delete_FacilityWithoutBookings_Succeeds_AndCascadesItsFacilitySportRowsOnly()
    {
        var facilityId = await CreateThrowawayFacilityAsync();
        await using (var setupContext = CreateContext())
        {
            var setupController = CreateFacilitiesController(setupContext);
            await setupController.Sports(facilityId, new List<int> { 1 }); // Tennis
        }

        try
        {
            await using var context = CreateContext();
            var controller = CreateFacilitiesController(context);

            var result = await controller.Delete(facilityId);

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            Assert.Null(await verify.Facilities.FindAsync(facilityId));
            Assert.Empty(await verify.FacilitySports.Where(fs => fs.FacilityId == facilityId).ToListAsync());
            // The Sport master row itself must never be touched by a Facility delete.
            Assert.NotNull(await verify.Sports.FindAsync(1));
        }
        finally
        {
            await DeleteFacilityIfExistsAsync(facilityId);
        }
    }

    [Fact]
    public async Task Delete_NonexistentFacility_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateFacilitiesController(context);

        var result = await controller.Delete(999999);

        Assert.IsType<NotFoundResult>(result);
    }
}
