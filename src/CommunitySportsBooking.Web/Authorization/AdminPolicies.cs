using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CommunitySportsBooking.Web.Authorization;

// One shared policy definition, two call sites (Program.cs registers it,
// AdminFunctionalityTests exercises it directly via IAuthorizationService) —
// same "single source of truth" reasoning as FacilityAvailabilityService.
// Keeps the test from silently drifting out of sync with what the app
// actually enforces.
public static class AdminPolicies
{
    public const string AdminOnly = "AdminOnly";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    }
}
