using PortalCalendarServer.Data;

namespace PortalCalendarServer.Services.Caches;

public interface IDatabaseCacheServiceFactory
{
    DatabaseCacheService Create(string creator, int minimalCacheExpiry = 0);
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

    public DatabaseCacheService Create(string creator, int minimalCacheExpiry = 0)
    {
        return new DatabaseCacheService(_context, _logger, creator, minimalCacheExpiry);
    }
}