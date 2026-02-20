namespace PortalCalendarServer.Models.Entities;

public class Theme
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool HasCustomConfig { get; set; } = false;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Display> Displays { get; set; } = new List<Display>();
}
