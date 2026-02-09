using Microsoft.Playwright;

namespace PortalCalendarServer.Services;

/// <summary>
/// Service to convert web pages to PNG images using Playwright.
/// Adapted from the Perl Web2Png module.
/// </summary>
public class Web2PngService : IAsyncDisposable
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

        try
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                DeviceScaleFactor = 1
            });

            try
            {
                var page = await context.NewPageAsync();
                
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
            throw;
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
