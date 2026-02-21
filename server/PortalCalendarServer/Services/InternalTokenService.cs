namespace PortalCalendarServer.Services;

/// <summary>
/// Generates a random, transient internal token at startup.
/// Used by PageGenerator to authenticate its internal HTTP requests
/// without requiring a user login session.
/// The token is regenerated every time the application restarts.
/// </summary>
public class InternalTokenService
{
    public string Token { get; } = Guid.NewGuid().ToString("N");
}
