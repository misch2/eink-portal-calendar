using Microsoft.AspNetCore.Mvc;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Controllers;

/// <summary>
/// API endpoints for cache management and diagnostics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Cache Management")]
public class CacheController : ControllerBase
{
    private readonly CacheManagementService _cacheManagement;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        CacheManagementService cacheManagement,
        ILogger<CacheController> logger)
    {
        _cacheManagement = cacheManagement;
        _logger = logger;
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(CacheStatistics), 200)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var stats = await _cacheManagement.GetCacheStatisticsAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Get detailed cache entries (limited to first 100 by default)
    /// </summary>
    [HttpGet("entries")]
    [ProducesResponseType(typeof(List<CacheEntryInfo>), 200)]
    public async Task<IActionResult> GetEntries(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var entries = await _cacheManagement.GetCacheEntriesAsync(limit, cancellationToken);
        return Ok(entries);
    }

    /// <summary>
    /// Clear all database caches
    /// </summary>
    [HttpDelete("all")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ClearAllCaches(CancellationToken cancellationToken)
    {
        await _cacheManagement.ClearAllDatabaseCachesAsync(cancellationToken);
        return Ok(new { message = "All caches cleared successfully" });
    }

    /// <summary>
    /// Clear database cache for a specific creator (integration service)
    /// </summary>
    [HttpDelete("creator/{creator}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ClearCacheByCreator(
        string creator,
        CancellationToken cancellationToken)
    {
        await _cacheManagement.ClearDatabaseCacheByCreatorAsync(creator, cancellationToken);
        return Ok(new { message = $"Cache cleared for creator: {creator}" });
    }

    /// <summary>
    /// Clear expired cache entries
    /// </summary>
    [HttpDelete("expired")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ClearExpiredCaches(CancellationToken cancellationToken)
    {
        await _cacheManagement.ClearExpiredCachesAsync(cancellationToken);
        return Ok(new { message = "Expired caches cleared successfully" });
    }
}
