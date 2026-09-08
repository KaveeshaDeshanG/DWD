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

// Admin Panel: Inquiry Management. Same discipline as every other test class
// here — real CommunitySportsBookingDB, no mocking. Read-only assertions
// anchor on seeded inquiries (Grace Lee/New, Henry Wu/Reviewed, Isla Brown/
// Closed — database/05_SeedData.sql), never mutated. UpdateStatus tests use
// a disposable throwaway inquiry.
[Collection("Database collection")]
public class AdminInquiryManagementTests
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

    private static async Task<int> CreateThrowawayInquiryAsync()
    {
        await using var context = CreateContext();
        var inquiry = new Inquiry
        {
            Name = "Throwaway Tester",
            Email = $"admin.inquiry.test.{Guid.NewGuid():N}@example.com",
            Subject = "Test subject",
            Message = "Test message body."
        };
        context.Inquiries.Add(inquiry);
        await context.SaveChangesAsync();
        return inquiry.InquiryId;
    }

    private static async Task DeleteInquiryIfExistsAsync(int inquiryId)
    {
        await using var context = CreateContext();
        var row = await context.Inquiries.FindAsync(inquiryId);
        if (row is not null)
        {
            context.Inquiries.Remove(row);
            await context.SaveChangesAsync();
        }
    }

    private sealed class NoOpTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static InquiriesController CreateInquiriesController(AppDbContext context)
    {
        var httpContext = new DefaultHttpContext();
        return new InquiriesController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NoOpTempDataProvider())
        };
    }

    [Fact]
    public async Task Index_NoFilters_ReturnsAllInquiriesFromDatabase()
    {
        await using var context = CreateContext();
        var controller = CreateInquiriesController(context);

        var result = await controller.Index(search: null, status: null);

        var model = Assert.IsType<InquiryIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        await using var verify = CreateContext();
        Assert.Equal(await verify.Inquiries.CountAsync(), model.Inquiries.Count);
    }

    [Fact]
    public async Task Index_SearchByName_ReturnsMatchingSeededInquiry()
    {
        await using var context = CreateContext();
        var controller = CreateInquiriesController(context);

        var result = await controller.Index(search: "Grace Lee", status: null);

        var model = Assert.IsType<InquiryIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Contains(model.Inquiries, i => i.Name == "Grace Lee" && i.Status == "New");
    }

    [Fact]
    public async Task Index_FilterByStatus_ReturnsOnlyThatStatus()
    {
        await using var context = CreateContext();
        var controller = CreateInquiriesController(context);

        var result = await controller.Index(search: null, status: "Closed");

        var model = Assert.IsType<InquiryIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotEmpty(model.Inquiries);
        Assert.All(model.Inquiries, i => Assert.Equal("Closed", i.Status));
        Assert.Contains(model.Inquiries, i => i.Name == "Isla Brown"); // seeded Closed inquiry
    }

    [Fact]
    public async Task Details_SeededInquiry_ReturnsFullMessage()
    {
        await using var context = CreateContext();
        var graceId = await context.Inquiries
            .Where(i => i.Name == "Grace Lee")
            .Select(i => i.InquiryId)
            .FirstAsync();
        var controller = CreateInquiriesController(context);

        var result = await controller.Details(graceId);

        var model = Assert.IsType<InquiryDetailsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Membership fees", model.Subject);
        Assert.False(string.IsNullOrWhiteSpace(model.Message));
    }

    [Fact]
    public async Task Details_NonexistentInquiry_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateInquiriesController(context);

        var result = await controller.Details(999999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ValidStatus_PersistsChange()
    {
        var inquiryId = await CreateThrowawayInquiryAsync();
        try
        {
            await using var context = CreateContext();
            var controller = CreateInquiriesController(context);

            var result = await controller.UpdateStatus(inquiryId, "Reviewed");

            Assert.IsType<RedirectToActionResult>(result);
            await using var verify = CreateContext();
            Assert.Equal("Reviewed", (await verify.Inquiries.FindAsync(inquiryId))!.Status);
        }
        finally
        {
            await DeleteInquiryIfExistsAsync(inquiryId);
        }
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_IsRejected_DatabaseUnchanged()
    {
        var inquiryId = await CreateThrowawayInquiryAsync();
        try
        {
            await using var context = CreateContext();
            var controller = CreateInquiriesController(context);

            var result = await controller.UpdateStatus(inquiryId, "BogusStatus");

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Invalid status.", controller.TempData["ErrorMessage"]);
            await using var verify = CreateContext();
            Assert.Equal("New", (await verify.Inquiries.FindAsync(inquiryId))!.Status);
        }
        finally
        {
            await DeleteInquiryIfExistsAsync(inquiryId);
        }
    }

    [Fact]
    public async Task UpdateStatus_NonexistentInquiry_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var controller = CreateInquiriesController(context);

        var result = await controller.UpdateStatus(999999, "Reviewed");

        Assert.IsType<NotFoundResult>(result);
    }
}
