namespace Whitestone.SegnoSharp.ViewModels.Security;

public sealed class RoleListItem
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public bool IsSystem { get; init; }
    public int MappingCount { get; init; }
    public int PermissionCount { get; init; }
    public bool HasWildcard { get; init; }
}