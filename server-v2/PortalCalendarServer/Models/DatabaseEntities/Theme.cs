using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace PortalCalendarServer.Models.Entities;

[Index(nameof(FileName), IsUnique = true)]

public class Theme
{
    [Key]
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool HasCustomConfig { get; set; } = false;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

}
