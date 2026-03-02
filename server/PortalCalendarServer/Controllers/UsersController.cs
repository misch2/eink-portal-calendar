using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalCalendarServer.Services;
using System.Security.Claims;

namespace PortalCalendarServer.Controllers;

[Controller]
[Authorize]
public class UsersController(UserService userService) : Controller
{
    // GET /users
    [HttpGet("/users")]
    public async Task<IActionResult> Index()
    {
        var users = await userService.GetAllUsersAsync();

        ViewData["NavLink"] = "users";
        ViewData["Title"] = "Users";

        return View("~/Views/Users/Index.cshtml", users);
    }

    // POST /users/add
    [HttpPost("/users/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser([FromForm] string username, [FromForm] string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Username and password are required.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await userService.GetUserByUsernameAsync(username);
        if (existing != null)
        {
            TempData["Error"] = $"User '{username}' already exists.";
            return RedirectToAction(nameof(Index));
        }

        await userService.CreateUserAsync(username, password);
        TempData["Message"] = $"User '{username}' created.";
        return RedirectToAction(nameof(Index));
    }

    // POST /users/delete/{id}
    [HttpPost("/users/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (id == currentUserId)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var userCount = await userService.GetUserCountAsync();
        if (userCount <= 1)
        {
            TempData["Error"] = "Cannot delete the last user.";
            return RedirectToAction(nameof(Index));
        }

        var user = await userService.GetUserByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        await userService.DeleteUserAsync(id);
        TempData["Message"] = $"User '{user.Username}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    // GET /change-password
    [HttpGet("/change-password")]
    public IActionResult ChangePassword()
    {
        ViewData["NavLink"] = "change-password";
        ViewData["Title"] = "Change Password";

        return View("~/Views/Users/ChangePassword.cshtml");
    }

    // POST /change-password
    [HttpPost("/change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePasswordPost(
        [FromForm] string currentPassword,
        [FromForm] string newPassword,
        [FromForm] string confirmPassword)
    {
        ViewData["NavLink"] = "change-password";
        ViewData["Title"] = "Change Password";

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["Error"] = "New password is required.";
            return View("~/Views/Users/ChangePassword.cshtml");
        }

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "New password and confirmation do not match.";
            return View("~/Views/Users/ChangePassword.cshtml");
        }

        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var username = User.FindFirstValue(ClaimTypes.Name)!;

        var user = await userService.AuthenticateAsync(username, currentPassword);
        if (user == null)
        {
            TempData["Error"] = "Current password is incorrect.";
            return View("~/Views/Users/ChangePassword.cshtml");
        }

        await userService.ChangePasswordAsync(currentUserId, newPassword);
        TempData["Message"] = "Password changed successfully.";
        return RedirectToAction(nameof(ChangePassword));
    }
}
