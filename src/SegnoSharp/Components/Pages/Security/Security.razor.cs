using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Configuration.Models;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Services;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.ViewModels.Security;

namespace Whitestone.SegnoSharp.Components.Pages.Security;

public partial class Security
{
    private const string RolesPage = "/admin/security/roles";
    private const string ApiClientsPage = "/admin/security/api-clients";

    [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
    [Inject] private PermissionRegistry Registry { get; set; }
    [Inject] private SecurityRolesSnapshotProvider Snapshots { get; set; }
    [Inject] private UnmappedRoleClaimTracker UnmappedClaims { get; set; }
    [Inject] private IOptions<SegnoSharpOpenIdConnectOptions> SecurityOptions { get; set; }
    [Inject] private ISystemClock Clock { get; set; }
    [Inject] private NavigationManager Navigation { get; set; }

    private OverviewStats _stats = new();
    private List<Finding> _findings = [];
    private bool _loading = true;
    private bool _busy;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task RefreshAsync()
    {
        _busy = true;

        try
        {
            await LoadAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task LoadAsync()
    {
        DateTime now = Clock.UtcNow;
        DateTime soon = now.AddDays(30);

        await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

        int rolesTotal = await db.SecurityRoles.CountAsync();
        int mappingsTotal = await db.SecurityRoles.SelectMany(role => role.IdpMappings).CountAsync();

        int bootstrapMappings = SecurityOptions.Value.AdminRole.Count;

        // A system role with no database mapping is still reachable through the bootstrap
        // setting, so it only counts as unreachable when that is empty too.
        int rolesUnreachable = await db.SecurityRoles
            .CountAsync(role => role.IdpMappings.Count == 0 && (!role.IsSystem || bootstrapMappings == 0));

        int clientsTotal = await db.SecurityApiClients.CountAsync();
        int clientsDisabled = await db.SecurityApiClients.CountAsync(client => !client.Enabled);

        int clientsWithoutActiveKey = await db.SecurityApiClients
            .CountAsync(client => client.Enabled && !client.Keys.Any(key =>
                key.Revoked == null && (key.Expires == null || key.Expires > now)));

        int keysActive = await db.SecurityApiKeys
            .CountAsync(key => key.Revoked == null && (key.Expires == null || key.Expires > now));

        int keysExpiringSoon = await db.SecurityApiKeys
            .CountAsync(key => key.Revoked == null
                               && key.Expires != null
                               && key.Expires > now
                               && key.Expires <= soon);

        int keysInactive = await db.SecurityApiKeys
            .CountAsync(key => key.Revoked != null || (key.Expires != null && key.Expires <= now));

        int keysNeverUsed = await db.SecurityApiKeys
            .CountAsync(key => key.LastUsed == null && key.Revoked == null);

        // Granted through navigation properties rather than the join DbSets, so this works
        // whichever way the context exposes them.
        List<string> grantedToRoles = await db.SecurityRoles
            .SelectMany(role => role.Permissions)
            .Select(permission => permission.Permission)
            .Distinct()
            .ToListAsync();

        List<string> grantedToClients = await db.SecurityApiClients
            .SelectMany(client => client.Permissions)
            .Select(permission => permission.Permission)
            .Distinct()
            .ToListAsync();

        int orphaned = grantedToRoles
            .Concat(grantedToClients)
            .Distinct(StringComparer.Ordinal)
            .Count(permission => permission != PermissionRegistry.Wildcard && !Registry.Contains(permission));

        SecurityRolesSnapshot snapshot = Snapshots.Current;
        int unmappedSeen = UnmappedClaims.Recent().Count;

        _stats = new OverviewStats
        {
            RolesTotal = rolesTotal,
            RolesUnreachable = rolesUnreachable,
            MappingsTotal = mappingsTotal,

            PermissionsDeclared = Registry.All.Count,
            PermissionProviders = Registry.ByPlugin().Count(),
            PermissionsForClients = Registry.All.Count(p => p.Permission.AllowForApiClients),
            OrphanedPermissions = orphaned,

            ClientsTotal = clientsTotal,
            ClientsDisabled = clientsDisabled,
            ClientsWithoutActiveKey = clientsWithoutActiveKey,

            KeysActive = keysActive,
            KeysExpiringSoon = keysExpiringSoon,
            KeysInactive = keysInactive,
            KeysNeverUsed = keysNeverUsed,

            RoleClaimType = SecurityOptions.Value.RoleClaim,
            BootstrapMappings = bootstrapMappings,
            SnapshotRoles = snapshot.Roles.Count,
            SnapshotClaimMappings = snapshot.ClaimToRoles.Count,
            UnmappedClaimsSeen = unmappedSeen
        };

        _findings = BuildFindings(_stats);
        _loading = false;
    }

    private static List<Finding> BuildFindings(OverviewStats stats)
    {
        List<Finding> findings = [];

        if (stats.BootstrapMappings == 0)
        {
            findings.Add(new Finding(
                "No bootstrap admin mapping is configured in appsettings. If the roles table is " +
                "damaged or the wrong mapping is removed, nobody can sign in with admin access.",
                "alert-error", null, null));
        }

        if (stats.RolesTotal == 0)
        {
            findings.Add(new Finding(
                "No roles are defined, so no user can be granted any permission.",
                "alert-warning", RolesPage, "Add a role"));
        }
        else if (stats.RolesUnreachable > 0)
        {
            findings.Add(new Finding(
                $"{stats.RolesUnreachable} " +
                $"{(stats.RolesUnreachable == 1 ? "role has" : "roles have")} no identity provider " +
                "role mapped, so nobody can reach them.",
                "alert-warning", RolesPage, "Review roles"));
        }

        if (stats.OrphanedPermissions > 0)
        {
            findings.Add(new Finding(
                $"{stats.OrphanedPermissions} granted " +
                $"{(stats.OrphanedPermissions == 1 ? "permission is" : "permissions are")} not declared " +
                "by any loaded plugin. They grant nothing while the owning plugin is missing or disabled.",
                "alert-warning", RolesPage, "Review roles"));
        }

        if (stats.KeysExpiringSoon > 0)
        {
            findings.Add(new Finding(
                $"{stats.KeysExpiringSoon} API " +
                $"{(stats.KeysExpiringSoon == 1 ? "key expires" : "keys expire")} within 30 days. " +
                "Issue a replacement and move the caller across before the old key stops working.",
                "alert-warning", ApiClientsPage, "Review keys"));
        }

        if (stats.ClientsWithoutActiveKey > 0)
        {
            findings.Add(new Finding(
                $"{stats.ClientsWithoutActiveKey} enabled " +
                $"{(stats.ClientsWithoutActiveKey == 1 ? "client has" : "clients have")} no usable key, " +
                "so they cannot authenticate.",
                "alert-info", ApiClientsPage, "Issue a key"));
        }

        if (stats.UnmappedClaimsSeen > 0)
        {
            findings.Add(new Finding(
                $"{stats.UnmappedClaimsSeen} identity provider role " +
                $"{(stats.UnmappedClaimsSeen == 1 ? "value has" : "values have")} been presented at " +
                "sign-in without matching any mapping. Map them if they should grant access.",
                "alert-info", RolesPage, "View values"));
        }

        if (stats.PermissionsDeclared == 0)
        {
            findings.Add(new Finding(
                "No permissions are declared. Check that the host and its plugins registered their " +
                "permission providers at startup.",
                "alert-error", null, null));
        }

        return findings;
    }
}
