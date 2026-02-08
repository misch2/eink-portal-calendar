using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Models;

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
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // For serving static content (CSS, JS, images)

app.UseRouting();

// Map controllers (both API and MVC)
app.MapControllers();

app.Run();
