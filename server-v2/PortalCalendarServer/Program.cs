using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PortalCalendarServer.Data;
using PortalCalendarServer.Controllers.ModelBinders;
using PortalCalendarServer.Models.ColorTypes;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.PageGeneratorComponents;
using PortalCalendarServer.Services.Integrations;
using Scalar.AspNetCore;
using System.Globalization;
using PortalCalendarServer.Services.Caches;

var builder = WebApplication.CreateBuilder(args);

// Configure the SQLite connection string to use an absolute path
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var relativePath = rawConnectionString!.Replace("Data Source=", "");
var absolutePath = Path.Combine(builder.Environment.ContentRootPath, "..", relativePath);
var absoluteConnectionString = $"Data Source={absolutePath}";
builder.Services.AddDbContext<CalendarContext>(options =>
    options.UseSqlite(absoluteConnectionString));

// Add services to the container
// Support for both API and MVC controllers
builder.Services.AddControllersWithViews(options =>
    options.ModelBinderProviders.Insert(0, new FlexibleBoolBinderProvider())
    );

// Add memory cache for HTTP response caching
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100 * 1024 * 1024; // 100MB cache limit
});

// Configure HttpClient with caching
builder.Services.AddHttpClient();
builder.Services.AddTransient<CachingHttpMessageHandler>();

// Register services
builder.Services.AddScoped<DisplayService>();
builder.Services.AddScoped<PageGeneratorService>();
builder.Services.AddScoped<CacheManagementService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddSingleton<Web2PngService>();
builder.Services.AddSingleton<ColorTypeRegistry>();

// Register background services
builder.Services.AddHostedService<CacheCleanupService>();

// Register integration services
builder.Services.AddScoped<NameDayService>();
builder.Services.AddScoped<IPublicHolidayService, PublicHolidayService>();
builder.Services.AddScoped<GoogleFitIntegrationService>();
// Add other integration services here as you create them:
// builder.Services.AddScoped<WeatherIntegrationService>();
// builder.Services.AddScoped<ICalIntegrationService>();

// Configure localization to not disturb number formatting in HTML forms etc.
var invariant = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = invariant;
CultureInfo.DefaultThreadCurrentUICulture = invariant;

var dateCulture = builder.Configuration["Culture:DateCulture"] ?? "en-US";

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
        logger.LogInformation("Starting database migrations");
        // Apply database migrations
        var context = services.GetRequiredService<CalendarContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
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

app.UseHttpsRedirection();
app.UseStaticFiles(); // For serving static content (CSS, JS, images)

app.UseRouting();

// Map controllers (both API and MVC)
app.MapControllers();

app.Run();
