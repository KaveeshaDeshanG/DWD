using System.Security.Claims;

namespace CommunitySportsBooking.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Reads the MemberId claim AccountController.Login already sets on sign-in.
    // Used everywhere a write must be scoped to the caller's own record, never
    // a client-supplied identifier (spec.md FR-011).
    public static int GetMemberId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (value is null || !int.TryParse(value, out var memberId))
        {
            throw new InvalidOperationException("Authenticated user has no valid MemberId claim.");
        }

        return memberId;
    }
}
