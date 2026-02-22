using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PortalCalendarServer.Data;

namespace PortalCalendarServer.Services;

/// <summary>
/// Stores authentication tickets in a SQLite database instead of the cookie,
/// keeping the cookie small (just a session key). Survives app restarts.
/// </summary>
public class SqliteTicketStore : ITicketStore
{
    private const string KeyPrefix = "auth-";
    private readonly IServiceScopeFactory _scopeFactory;

    public SqliteTicketStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = KeyPrefix + Guid.NewGuid().ToString("N");
        await UpsertAsync(key, ticket);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        await UpsertAsync(key, ticket);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SessionContext>();

        var session = await db.AuthSessions.FindAsync(key);
        if (session == null || session.ExpiresAt < DateTimeOffset.UtcNow)
            return null;

        return TicketSerializer.Default.Deserialize(session.Value);
    }

    public async Task RemoveAsync(string key)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SessionContext>();

        var session = await db.AuthSessions.FindAsync(key);
        if (session != null)
        {
            db.AuthSessions.Remove(session);
            await db.SaveChangesAsync();
        }
    }

    private async Task UpsertAsync(string key, AuthenticationTicket ticket)
    {
        var expiresAt = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddDays(365);
        var value = TicketSerializer.Default.Serialize(ticket);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SessionContext>();

        var existing = await db.AuthSessions.FindAsync(key);
        if (existing == null)
        {
            db.AuthSessions.Add(new AuthSession { Key = key, Value = value, ExpiresAt = expiresAt });
        }
        else
        {
            existing.Value = value;
            existing.ExpiresAt = expiresAt;
        }

        await db.SaveChangesAsync();
    }
}
