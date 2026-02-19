using PortalCalendarServer.Data;

namespace PortalCalendarServer.Services.Caches;

public interface IDatabaseCacheServiceFactory
{
    DatabaseCacheService Create(string creator, TimeSpan expiration);
}

public class DatabaseCacheServiceFactory : IDatabaseCacheServiceFactory
{
    private readonly CalendarContext _context;
    private readonly ILogger<DatabaseCacheService> _logger;

    public DatabaseCacheServiceFactory(
        CalendarContext context,
        ILogger<DatabaseCacheService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public DatabaseCacheService Create(string creator, TimeSpan expiration)
    {
        return new DatabaseCacheService(_context, _logger, creator, expiration);
    }
}