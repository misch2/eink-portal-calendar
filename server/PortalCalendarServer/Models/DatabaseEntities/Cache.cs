namespace PortalCalendarServer.Models.DatabaseEntities;

public partial class Cache
{
    public int Id { get; set; }

    public string Creator { get; set; } = null!;

    public string Key { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public byte[]? Data { get; set; }
}
