namespace PortalCalendarServer.Models.DatabaseEntities;

public class AppUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsAdmin { get; set; }

    public ICollection<Display> OwnedDisplays { get; set; } = null!;
    public ICollection<Gallery> OwnedGalleries { get; set; } = null!;
}
