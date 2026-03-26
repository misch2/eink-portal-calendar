namespace PortalCalendarServer.Services;

/// <summary>
/// Service interface for converting web pages to PNG images
/// </summary>
public interface IWeb2PngService : IAsyncDisposable
{
    /// <summary>
    /// Convert a URL to a PNG file.
    /// </summary>
    /// <param name="url">The URL to capture</param>
    /// <param name="width">Width of the viewport/screenshot</param>
    /// <param name="height">Height of the viewport/screenshot</param>
    /// <param name="destinationPath">Path where the PNG file should be saved</param>
    /// <param name="delayMs">Delay in milliseconds to wait for web fonts and rendering (default: 2000ms)</param>
    /// <param name="extraHeaders">Optional HTTP headers to send with the page request (e.g. internal auth token)</param>
    /// <param name="bypassCsp">When true, Playwright ignores Content-Security-Policy headers on the page.
    /// This prevents a stale or overly-restrictive CSP from blocking external resources
    /// (e.g. Google Fonts) that the page legitimately needs.</param>
    /// <param name="tolerateNetworkErrors">When true, network errors (failed requests, HTTP 4xx/5xx)
    /// are logged as warnings instead of causing the method to throw.
    /// Useful for best-effort rendering such as error fallback pages.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ConvertUrlAsync(
        string url,
        int width,
        int height,
        string destinationPath,
        int delayMs = 2000,
        Dictionary<string, string>? extraHeaders = null,
        bool bypassCsp = true,
        bool tolerateNetworkErrors = false,
        CancellationToken cancellationToken = default);
}
