using System.Security.Claims;

namespace PortalCalendarServer.Services;

public class CurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public int? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return value != null ? int.Parse(value) : null;
        }
    }

    public bool IsAdmin => httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
