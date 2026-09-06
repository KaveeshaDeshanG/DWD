using System.ComponentModel.DataAnnotations;
using CommunitySportsBooking.Web.Controllers;
using CommunitySportsBooking.Web.Data;
using CommunitySportsBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunitySportsBooking.Tests;

// Same discipline as the other functionality test classes: real
// CommunitySportsBookingDB, every inserted row cleaned up. InquiryController
// has no [Authorize] and never reads User, but its success path DOES write
// TempData["SuccessMessage"] — found by running this test for real: a bare
// `new InquiryController(context)` with no ControllerContext throws
// NullReferenceException the instant TempData is touched, since
// Controller.TempData resolves through HttpContext.RequestServices. Same
// NoOpTempDataProvider wiring as ReviewFunctionalityTests.
[Collection("Database collection")]
public class InquiryFunctionalityTests
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

    private sealed class NoOpTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static InquiryController CreateInquiryController(AppDbContext context)
    {
        var httpContext = new DefaultHttpContext();
        return new InquiryController(context)
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

    [Fact]
    public async Task Create_Post_ValidSubmission_PersistsWithNewStatus()
    {
        await using var context = CreateContext();
        var controller = CreateInquiryController(context);
        var email = $"test.inquiry.{Guid.NewGuid():N}@example.com";
        var model = new InquiryViewModel
        {
            Name = "Test Guest",
            Email = email,
            Subject = "Test Subject",
            Message = "This row is inserted and deleted by an automated test."
        };
        ApplyRealValidation(controller, model);
        Assert.True(controller.ModelState.IsValid);

        int? insertedId = null;
        try
        {
            var result = await controller.Create(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(InquiryController.Create), redirect.ActionName);

            await using var verify = CreateContext();
            var row = await verify.Inquiries.SingleOrDefaultAsync(i => i.Email == email);
            Assert.NotNull(row);
            Assert.Equal("New", row!.Status); // never client-settable — controller never assigns Status
            Assert.Equal(model.Subject, row.Subject);
            Assert.Equal(model.Message, row.Message);
            insertedId = row.InquiryId;
        }
        finally
        {
            if (insertedId is not null)
            {
                await using var cleanup = CreateContext();
                var toDelete = await cleanup.Inquiries.FindAsync(insertedId);
                if (toDelete is not null)
                {
                    cleanup.Inquiries.Remove(toDelete);
                    await cleanup.SaveChangesAsync();
                }
            }
        }
    }

    [Fact]
    public async Task Create_Post_MissingRequiredFields_RealValidationRejectsIt_NoRowCreated()
    {
        await using var context = CreateContext();
        var beforeCount = await context.Inquiries.CountAsync();
        var controller = CreateInquiryController(context);
        var model = new InquiryViewModel { Name = "", Email = "", Subject = "", Message = "" };
        ApplyRealValidation(controller, model);
        Assert.False(controller.ModelState.IsValid);

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        await using var verify = CreateContext();
        Assert.Equal(beforeCount, await verify.Inquiries.CountAsync());
    }

    [Fact]
    public async Task Create_Post_InvalidEmailFormat_RealValidationRejectsIt_NoRowCreated()
    {
        await using var context = CreateContext();
        var beforeCount = await context.Inquiries.CountAsync();
        var controller = CreateInquiryController(context);
        var model = new InquiryViewModel { Name = "Test Guest", Email = "not-an-email", Subject = "Subject", Message = "Message body." };
        ApplyRealValidation(controller, model);
        Assert.False(controller.ModelState.IsValid);

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        await using var verify = CreateContext();
        Assert.Equal(beforeCount, await verify.Inquiries.CountAsync());
    }
}
