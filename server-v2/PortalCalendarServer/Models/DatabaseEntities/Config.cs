using System;
using System.Collections.Generic;

namespace PortalCalendarServer.Models.Entities;

public partial class Config
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public int DisplayId { get; set; }

    public virtual Display Display { get; set; } = null!;
}
