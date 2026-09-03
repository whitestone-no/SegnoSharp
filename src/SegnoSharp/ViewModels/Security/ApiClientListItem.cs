using System;

namespace Whitestone.SegnoSharp.ViewModels.Security;

public sealed class ApiClientListItem
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public bool Enabled { get; init; }
    public DateTime Created { get; init; }
    public int ActiveKeyCount { get; init; }
    public int PermissionCount { get; init; }
}