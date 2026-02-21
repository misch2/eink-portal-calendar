using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using PortalCalendarServer.Models.POCOs;
using System.Security.Claims;

namespace PortalCalendarServer.Controllers;

[Controller]
public class LoginController(IConfiguration configuration) : Controller
{
    [HttpGet("/login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(UiController.SelectDisplay), "Ui");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] string username,
        [FromForm] string password,
        [FromQuery] string? returnUrl = null)
    {
        var users = configuration.GetSection("Auth:Users").Get<List<AuthUser>>() ?? [];
        var match = users.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (match != null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, match.Username)
            };
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(365)
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(UiController.SelectDisplay), "Ui");
        }

        ViewData["Error"] = "Invalid username or password.";
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return RedirectToAction(nameof(Login));
    }
}
