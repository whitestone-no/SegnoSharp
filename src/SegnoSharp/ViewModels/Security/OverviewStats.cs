namespace Whitestone.SegnoSharp.ViewModels.Security;

public class OverviewStats
{
    public int RolesTotal { get; init; }
    public int RolesUnreachable { get; init; }
    public int MappingsTotal { get; init; }

    public int PermissionsDeclared { get; init; }
    public int PermissionProviders { get; init; }
    public int PermissionsForClients { get; init; }
    public int OrphanedPermissions { get; init; }

    public int ClientsTotal { get; init; }
    public int ClientsDisabled { get; init; }
    public int ClientsWithoutActiveKey { get; init; }

    public int KeysActive { get; init; }
    public int KeysExpiringSoon { get; init; }
    public int KeysInactive { get; init; }
    public int KeysNeverUsed { get; init; }

    public string RoleClaimType { get; init; } = "";
    public int BootstrapMappings { get; init; }
    public int SnapshotRoles { get; init; }
    public int SnapshotClaimMappings { get; init; }
    public int UnmappedClaimsSeen { get; init; }
}