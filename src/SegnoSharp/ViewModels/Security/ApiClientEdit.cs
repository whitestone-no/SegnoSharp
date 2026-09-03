using System;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.ViewModels.Security;

public sealed class ApiClientEdit
{
    public int Id { get; init; }
    public string Name { get; set; } = "";
    public string Description { get; set; }
    public bool Enabled { get; set; } = true;
    public HashSet<string> Permissions { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Grants as loaded, so orphans can be kept but never newly added.</summary>
    public HashSet<string> OriginalPermissions { get; init; } = new(StringComparer.Ordinal);
}