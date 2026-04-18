using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using System.Security.Cryptography;

namespace PortalCalendarServer.Services;

public class UserService(CalendarContext context, ICurrentUserProvider currentUser)
{
    private readonly CalendarContext _context = context;

    /// <summary>
    /// Returns all users for admins, or only the current user for non-admins.
    /// </summary>
    public async Task<List<AppUser>> GetVisibleUsersAsync()
    {
        if (currentUser.IsAdmin)
            return await _context.Users.OrderBy(u => u.Id).ToListAsync();

        var user = await _context.Users.FindAsync(currentUser.UserId);
        return user != null ? [user] : [];
    }

    /// <summary>
    /// Returns the user if the current user is allowed to see them (admins see all, non-admins see only themselves).
    /// </summary>
    public async Task<AppUser?> GetVisibleUserByIdAsync(int id)
    {
        if (!currentUser.IsAdmin && id != currentUser.UserId)
            return null;

        return await _context.Users.FindAsync(id);
    }

    public async Task<AppUser?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<AppUser?> AuthenticateAsync(string username, string password)
    {
        var user = await GetUserByUsernameAsync(username);
        if (user == null)
        {
            return null;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public async Task<AppUser> CreateUserAsync(string username, string password)
    {
        RequireAdmin();

        var user = new AppUser
        {
            Username = username,
            PasswordHash = HashPassword(password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        RequireAdmin();

        if (id == currentUser.UserId)
            throw new UnauthorizedAccessException("You cannot delete your own account.");

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
    {
        if (!currentUser.IsAdmin && userId != currentUser.UserId)
            throw new UnauthorizedAccessException("You can only change your own password.");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUserCountAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task ToggleAdminAsync(int userId)
    {
        RequireAdmin();

        if (userId == currentUser.UserId)
            throw new UnauthorizedAccessException("You cannot change your own admin status.");

        var user = await _context.Users.FindAsync(userId)
            ?? throw new ArgumentException($"User {userId} not found");

        user.IsAdmin = !user.IsAdmin;
        await _context.SaveChangesAsync();
    }

    private void RequireAdmin()
    {
        if (!currentUser.IsAdmin)
            throw new UnauthorizedAccessException("This operation requires admin privileges.");
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100_000,
            numBytesRequested: 32);

        // Store as base64: salt + hash
        var combined = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, combined, salt.Length, hash.Length);
        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var combined = Convert.FromBase64String(storedHash);
        if (combined.Length != 48) // 16 bytes salt + 32 bytes hash
        {
            return false;
        }

        var salt = new byte[16];
        var storedHashBytes = new byte[32];
        Buffer.BlockCopy(combined, 0, salt, 0, 16);
        Buffer.BlockCopy(combined, 16, storedHashBytes, 0, 32);

        var computedHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100_000,
            numBytesRequested: 32);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
    }
}
