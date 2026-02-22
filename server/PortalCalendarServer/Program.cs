using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using PortalCalendarServer.Controllers.ModelBinders;
using PortalCalendarServer.Data;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.BackgroundJobs;
using PortalCalendarServer.Services.Caches;
using PortalCalendarServer.Services.Integrations;
using Scalar.AspNetCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
//builder.Logging.AddConsole(options =>
//{
//    options.FormatterName = "custom";
//});
//builder.Logging.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.IncludeScopes = false;
});

// Configure the SQLite connection string to use an absolute path
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var templatePath = rawConnectionString!.Replace("Data Source=", "");
var realPath = templatePath;
realPath = realPath.Replace("{ContentRootPath}", builder.Environment.ContentRootPath);
var absolutePath = Path.GetFullPath(realPath); // Ensure it's an absolute path

var absoluteConnectionString = $"Data Source={absolutePath}";
builder.Services.AddDbContext<CalendarContext>(options =>
    options.UseLazyLoadingProxies()
    .UseSqlite(absoluteConnectionString));

// Session store database (separate SQLite file)
var rawSessionConnectionString = builder.Configuration.GetConnectionString("SessionConnection");
var sessionPath = rawSessionConnectionString!.Replace("Data Source=", "")
    .Replace("{ContentRootPath}", builder.Environment.ContentRootPath);
var absoluteSessionPath = Path.GetFullPath(sessionPath);
var absoluteSessionConnectionString = $"Data Source={absoluteSessionPath}";
builder.Services.AddDbContext<SessionContext>(options =>
    options.UseSqlite(absoluteSessionConnectionString));

// Add services to the container
// Support for both API and MVC controllers
builder.Services.AddControllersWithViews(options =>
    options.ModelBinderProviders.Insert(0, new FlexibleBoolBinderProvider())
    );

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "PortalCalendarAntiforgery";
});

// Add memory cache for HTTP response caching
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100 * 1024 * 1024; // 100MB cache limit
});

// Configure HttpClient with caching
builder.Services.AddHttpClient();
builder.Services.AddTransient<CachingHttpMessageHandler>();

builder.Services.AddHttpClient(Options.DefaultName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent",
        "PortalCalendar/2.0 (github.com/misch2/eink-portal-calendar)");
})
.AddHttpMessageHandler<CachingHttpMessageHandler>();

// Register services
builder.Services.AddSingleton<IWeb2PngService, Web2PngService>();
builder.Services.AddScoped<IDisplayService, DisplayService>();
builder.Services.AddScoped<PageGeneratorService>();
builder.Services.AddScoped<CacheManagementService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<IDatabaseCacheServiceFactory, DatabaseCacheServiceFactory>();
builder.Services.AddScoped<IMqttService, MqttService>();
builder.Services.AddScoped<INameDayService, NameDayService>();
builder.Services.AddScoped<IPublicHolidayService, PublicHolidayService>();
builder.Services.AddScoped<GoogleFitIntegrationService>();

// Register periodic background services
builder.Services.AddHostedService<PortalCalendarServer.Services.BackgroundJobs.Periodic.CacheCleanupService>();
builder.Services.AddHostedService<PortalCalendarServer.Services.BackgroundJobs.Periodic.BitmapGenerationService>();
builder.Services.AddHostedService<PortalCalendarServer.Services.BackgroundJobs.Periodic.MissedConnectionsCheckService>();

// Register Image Regeneration Service (singleton + hosted)
builder.Services.AddSingleton<ImageRegenerationService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ImageRegenerationService>());

// Register SQLite-backed ticket store for cookie authentication
builder.Services.AddScoped<SqliteTicketStore>();

// Configure cookie authentication for the web UI
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Use a unique name for each environment
        options.Cookie.Name = builder.Configuration.GetValue<string>("Auth:CookieName") ?? "PortalCalendarAuth";
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
        options.SlidingExpiration = true;
        // SessionStore is set via PostConfigure below, after the DI container is fully built
    })
    .AddScheme<InternalTokenAuthenticationOptions, InternalTokenAuthenticationHandler>(
        InternalTokenAuthenticationHandler.SchemeName,
        _ => { });

// Set SessionStore after the DI container is fully built to avoid using
// a premature BuildServiceProvider() call which would bypass the real container
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .PostConfigure<IServiceScopeFactory>((options, scopeFactory) =>
    {
        options.SessionStore = new SqliteTicketStore(scopeFactory);
    });

builder.Services.AddAuthorization(options =>
{
    // Default policy: Cookies only, so unauthenticated browser requests get a 302 redirect to /login.
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("Cookies")
        .RequireAuthenticatedUser()
        .Build();

    // Used on calendar HTML endpoints so the internal page generator (Playwright) can also authenticate
    // via the X-Internal-Token header without a user cookie session.
    options.AddPolicy("CookiesOrInternalToken",
        new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes("Cookies", InternalTokenAuthenticationHandler.SchemeName)
            .RequireAuthenticatedUser()
            .Build());
});

// Register the singleton internal token service (must come before services that depend on it)
builder.Services.AddSingleton<InternalTokenService>();

// Configure localization to not disturb number formatting in HTML forms, date printing in logs etc.
var invariant = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariant;
CultureInfo.DefaultThreadCurrentUICulture = invariant;

// Configure OpenAPI with custom settings
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Portal Calendar API",
            Version = "v1",
            Description = "API for managing e-ink portal calendar displays",
            Contact = new OpenApiContact
            {
                Name = "Portal Calendar",
                Url = new Uri("https://github.com/misch2/eink-portal-calendar")
            }
        };

        // Organize tags
        document.Tags = new List<OpenApiTag>
        {
            new() { Name = "Health Checks", Description = "System health and connectivity checks" },
            new() { Name = "Device API", Description = "Endpoints called by e-ink devices" },
            new() { Name = "Display Configuration", Description = "Display settings and management" },
            new() { Name = "Image Generation", Description = "Bitmap and image generation" },
            new() { Name = "UI Management", Description = "Web UI for display management" },
            new() { Name = "Authentication", Description = "OAuth and authentication flows" }
        };

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<CalendarContext>();
        var connectionString = context.Database.GetConnectionString();
        logger.LogInformation("Starting database migrations on {absoluteConnectionString}", connectionString);
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");

        var sessionContext = services.GetRequiredService<SessionContext>();
        var sessionConnectionString = sessionContext.Database.GetConnectionString();
        logger.LogInformation("Starting session database migrations on {absoluteSessionConnectionString}", sessionConnectionString);
        await sessionContext.Database.MigrateAsync();
        logger.LogInformation("Session database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during startup initialization");
        throw;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // /openapi/v1.json - Raw OpenAPI specification (JSON)
    // /scalar/v1 - Beautiful interactive API documentation UI where you can test endpoints

    app.MapOpenApi(); // Serves the OpenAPI JSON at /openapi/v1.json

    // Optional: Add Scalar UI for interactive API documentation
    app.MapScalarApiReference(options =>
    {
        options.Title = "Portal Calendar API";
        options.Theme = ScalarTheme.DeepSpace;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    }); // Available at /scalar/v1
}

//// Use request localization
//app.UseRequestLocalization();

//app.UseHttpsRedirection();    // Not needed since this is typically run behind a reverse proxy that handles TLS termination
app.UseStaticFiles(); // For serving static content (CSS, JS, images)

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers (both API and MVC)
app.MapControllers();

app.Run();
