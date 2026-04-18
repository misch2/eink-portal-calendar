namespace PortalCalendarServer.Services;

public interface ICurrentUserProvider
{
    int? UserId { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}
