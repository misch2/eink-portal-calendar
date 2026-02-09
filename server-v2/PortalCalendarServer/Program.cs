using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PortalCalendarServer.Models;
using PortalCalendarServer.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure the SQLite connection string to use an absolute path
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var relativePath = rawConnectionString!.Replace("Data Source=", "");
var absolutePath = Path.Combine(builder.Environment.ContentRootPath, "..", relativePath);
var absoluteConnectionString = $"Data Source={absolutePath}";
builder.Services.AddDbContext<CalendarContext>(options =>
    options.UseSqlite(absoluteConnectionString));

// Add services to the container
builder.Services.AddControllersWithViews(); // Support for both API and MVC controllers

// Register services
builder.Services.AddScoped<DisplayService>();
builder.Services.AddScoped<ICalendarUtilFactory, CalendarUtilFactory>();

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

app.UseHttpsRedirection();
app.UseStaticFiles(); // For serving static content (CSS, JS, images)

app.UseRouting();

// Map controllers (both API and MVC)
app.MapControllers();

app.Run();
