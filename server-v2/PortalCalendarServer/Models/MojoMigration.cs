using System;
using System.Collections.Generic;

namespace PortalCalendarServer.Models;

public partial class MojoMigration
{
    public string Name { get; set; } = null!;

    public int Version { get; set; }
}
