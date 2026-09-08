using CommunitySportsBooking.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunitySportsBooking.Web.Areas.Admin.Controllers;

// Every Admin controller inherits this rather than repeating [Area]/[Authorize]
// on each one individually — one place owns "what it takes to be an Admin
// controller", matching the project's existing preference for one shared
// decision over duplicated attributes (e.g. FacilityAvailabilityService).
[Area("Admin")]
[Authorize(Policy = AdminPolicies.AdminOnly)]
public abstract class AdminControllerBase : Controller
{
}
