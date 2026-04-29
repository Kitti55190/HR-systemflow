using System.Security.Claims;

namespace HrFlow.Api.Services;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            throw new InvalidOperationException("Invalid user id.");
        }

        return userId;
    }
}
