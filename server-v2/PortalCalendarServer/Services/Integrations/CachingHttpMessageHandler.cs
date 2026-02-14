using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace PortalCalendarServer.Services.Integrations;
 
/// <summary>
/// HTTP message handler that provides caching for HTTP responses using IMemoryCache.
/// This is a lightweight alternative to LWP::UserAgent::Caching from Perl.
/// </summary>
public class CachingHttpMessageHandler : DelegatingHandler
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingHttpMessageHandler> _logger;
    private readonly int _defaultCacheSeconds;

    public CachingHttpMessageHandler(
        IMemoryCache cache,
        ILogger<CachingHttpMessageHandler> logger,
        int defaultCacheSeconds = 600) // 10 minutes default
    {
        _cache = cache;
        _logger = logger;
        _defaultCacheSeconds = defaultCacheSeconds;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Only cache GET requests
        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var cacheKey = $"http_cache:{request.RequestUri}";

        // Try to get from cache
        if (_cache.TryGetValue<CachedResponse>(cacheKey, out var cachedResponse))
        {
            _logger.LogDebug("Cache HIT for {Uri}", request.RequestUri);
            return CreateResponseFromCache(cachedResponse!);
        }

        _logger.LogDebug("Cache MISS for {Uri}", request.RequestUri);

        // Fetch from network
        var response = await base.SendAsync(request, cancellationToken);

        // Only cache successful responses
        if (response.IsSuccessStatusCode)
        {
            var cached = await CreateCachedResponseAsync(response);

            // Determine cache duration from Cache-Control header or use default
            var cacheSeconds = GetCacheDuration(response) ?? _defaultCacheSeconds;

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheSeconds),
                Size = cached.Content?.Length ?? 1024 // Estimate size for cache management
            };

            _cache.Set(cacheKey, cached, cacheOptions);
            _logger.LogDebug("Cached response for {Uri} for {Seconds} seconds", request.RequestUri, cacheSeconds);
        }

        return response;
    }

    private static async Task<CachedResponse> CreateCachedResponseAsync(HttpResponseMessage response)
    {
        return new CachedResponse
        {
            StatusCode = response.StatusCode,
            Content = await response.Content.ReadAsByteArrayAsync(),
            ContentType = response.Content.Headers.ContentType?.ToString(),
            Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToList())
        };
    }

    private static HttpResponseMessage CreateResponseFromCache(CachedResponse cached)
    {
        var response = new HttpResponseMessage(cached.StatusCode)
        {
            Content = new ByteArrayContent(cached.Content ?? Array.Empty<byte>())
        };

        if (cached.ContentType != null)
        {
            response.Content.Headers.ContentType =
                System.Net.Http.Headers.MediaTypeHeaderValue.Parse(cached.ContentType);
        }

        foreach (var header in cached.Headers)
        {
            response.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return response;
    }

    private static int? GetCacheDuration(HttpResponseMessage response)
    {
        if (response.Headers.CacheControl?.MaxAge.HasValue == true)
        {
            return (int)response.Headers.CacheControl.MaxAge.Value.TotalSeconds;
        }

        return null;
    }

    private class CachedResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public byte[]? Content { get; set; }
        public string? ContentType { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; } = new();
    }
}
