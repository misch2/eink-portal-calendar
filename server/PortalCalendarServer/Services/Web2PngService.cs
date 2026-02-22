using Microsoft.Playwright;

namespace PortalCalendarServer.Services;

/// <summary>
/// Service to convert web pages to PNG images using Playwright.
/// Adapted from the Perl Web2Png module.
/// </summary>
public class Web2PngService : IWeb2PngService, IAsyncDisposable
{
    private readonly ILogger<Web2PngService> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public Web2PngService(ILogger<Web2PngService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize Playwright and browser instance
    /// </summary>
    private async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            _logger.LogInformation("Initializing Playwright browser");
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--disable-dev-shm-usage", "--no-sandbox" }
            });
            _initialized = true;
            _logger.LogInformation("Playwright browser initialized successfully");
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Convert a URL to a PNG file.
    /// </summary>
    /// <param name="url">The URL to capture</param>
    /// <param name="width">Width of the viewport/screenshot</param>
    /// <param name="height">Height of the viewport/screenshot</param>
    /// <param name="destinationPath">Path where the PNG file should be saved</param>
    /// <param name="delayMs">Delay in milliseconds to wait for web fonts and rendering (default: 2000ms)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ConvertUrlAsync(
        string url,
        int width,
        int height,
        string destinationPath,
        int delayMs = 2000,
        Dictionary<string, string>? extraHeaders = null,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync();

        if (_browser == null)
        {
            throw new InvalidOperationException("Browser not initialized");
        }

        _logger.LogInformation("Converting URL to PNG: {Url} ({Width}x{Height})", url, width, height);

        // Create a temporary directory for the screenshot
        var tempDir = Path.Combine(Path.GetTempPath(), $"web2png_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "screenshot.png");

        // Determine the origin of the target URL so extra headers are only sent to same-origin requests
        var targetOrigin = new Uri(url).GetLeftPart(UriPartial.Authority);

        try
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                DeviceScaleFactor = 1
                // Do NOT set ExtraHTTPHeaders here — they would be sent on all
                // requests including cross-origin ones, breaking CORS for external
                // resources like Google Fonts.
            });

            try
            {
                var page = await context.NewPageAsync();

                // Intercept requests to add extra headers only to same-origin URLs.
                // This avoids CORS preflight failures on cross-origin resources
                // (e.g. fonts.gstatic.com rejecting the X-Internal-Token header).
                if (extraHeaders is { Count: > 0 })
                {
                    await page.RouteAsync("**/*", async route =>
                    {
                        var requestUrl = route.Request.Url;
                        var requestOrigin = new Uri(requestUrl).GetLeftPart(UriPartial.Authority);

                        if (string.Equals(requestOrigin, targetOrigin, StringComparison.OrdinalIgnoreCase))
                        {
                            await route.ContinueAsync(new RouteContinueOptions
                            {
                                Headers = new Dictionary<string, string>(route.Request.Headers)
                                    .Concat(extraHeaders)
                                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                            });
                        }
                        else
                        {
                            await route.ContinueAsync();
                        }
                    });
                }

                // Add console logging to diagnose rendering issues
                page.Console += (_, msg) =>
                {
                    if (msg.Type == "error")
                    {
                        _logger.LogError("Web2Png console error: {Text}", msg.Text);
                    }
                    else if (msg.Type == "warning")
                    {
                        _logger.LogWarning("Web2Png console warning: {Text}", msg.Text);
                    }
                    else
                    {
                        // log, debug, info, trace, etc.
                        _logger.LogDebug("Web2Png console [{Type}]: {Text}", msg.Type, msg.Text);
                    }
                };

                var networkErrors = new List<string>();

                page.PageError += (_, error) =>
                {
                    var msg = $"Page error: {error}";
                    _logger.LogError("Web2Png page issue {Error}", msg);
                    networkErrors.Add(msg);
                };


                page.RequestFailed += (_, request) =>
                {
                    var msg = $"Request failed: {request.Method} {request.Url} — {request.Failure}";
                    _logger.LogError("Web2Png request issue: {Message}", msg);
                    networkErrors.Add(msg);
                };

                page.Response += (_, response) =>
                {
                    if (response.Status >= 400)
                    {
                        var msg = $"HTTP {response.Status} {response.StatusText}: {response.Url}";
                        _logger.LogWarning("Web2Png response issue: {Message}", msg);
                        networkErrors.Add(msg);
                    }
                };

                try
                {
                    // Navigate to the URL
                    await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle,
                        Timeout = 30000 // 30 seconds timeout
                    });

                    // Wait for fonts and dynamic content to load
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }

                    // Take screenshot
                    await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = tempFile,
                        Type = ScreenshotType.Png,
                        FullPage = false // Use viewport size
                    });

                    _logger.LogInformation("Screenshot saved to temporary file: {TempFile}", tempFile);

                    // Ensure destination directory exists
                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // Copy to destination (overwrite if exists)
                    File.Copy(tempFile, destinationPath, overwrite: true);

                    _logger.LogInformation("PNG file created successfully: {DestinationPath}", destinationPath);

                    if (networkErrors.Count > 0)
                    {
                        //_logger.LogWarning(
                        //    "PNG created but {Count} network issue(s) were encountered during page load for {Url}",
                        //    networkErrors.Count, url);
                        throw new AggregateException(
                            $"PNG created but {networkErrors.Count} network issue(s) occurred while loading {url}",
                            networkErrors.Select(e => new HttpRequestException(e)));
                    }
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
            finally
            {
                await context.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert URL to PNG: {Url}", url);
            throw new InvalidOperationException($"Failed to convert URL to PNG: {url}", ex);
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary directory: {TempDir}", tempDir);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();

        _initLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
