namespace PortalCalendarServer.Models.DatabaseEntities;

public class AppUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
}
