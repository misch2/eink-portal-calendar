namespace PortalCalendarServer.Services;

/// <summary>
/// Tracks whether the server is in "pairing mode", which allows unknown ESP32 devices
/// to register and receive an API key. In-memory only — a server restart automatically
/// closes any open pairing window.
/// </summary>
public class PairingModeService
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);

    private readonly object _lock = new();
    private DateTimeOffset? _expiresAt;

    public bool IsActive
    {
        get { lock (_lock) return _expiresAt.HasValue && _expiresAt > DateTimeOffset.UtcNow; }
    }

    public int SecondsRemaining
    {
        get
        {
            lock (_lock)
                return _expiresAt.HasValue
                    ? Math.Max(0, (int)(_expiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds)
                    : 0;
        }
    }

    public void Activate()
    {
        lock (_lock)
            _expiresAt = DateTimeOffset.UtcNow + DefaultDuration;
    }

    public void Deactivate()
    {
        lock (_lock)
            _expiresAt = null;
    }
}
