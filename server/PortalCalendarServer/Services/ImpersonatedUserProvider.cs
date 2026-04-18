namespace PortalCalendarServer.Services;

/// <summary>
/// An <see cref="ICurrentUserProvider"/> that represents a specific user identity.
/// Used when performing operations on behalf of a display owner (e.g. during rendering).
/// </summary>
public class ImpersonatedUserProvider(int? userId) : ICurrentUserProvider
{
    public int? UserId => userId;
    public bool IsAdmin => false;
    public bool IsAuthenticated => userId != null;
}
