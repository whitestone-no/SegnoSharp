using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Configuration.Models;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Services;
using Whitestone.SegnoSharp.Shared.Permissions;
using Whitestone.SegnoSharp.ViewModels.Security;

namespace Whitestone.SegnoSharp.Components.Pages.Security;

public partial class Roles
{
    [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
    [Inject] private PermissionRegistry Registry { get; set; }
    [Inject] private SecurityRolesSnapshotProvider Snapshots { get; set; }
    [Inject] private IOptions<SegnoSharpOpenIdConnectOptions> Security { get; set; }
    [Inject] private UnmappedRoleClaimTracker UnmappedClaims { get; set; }
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Inject] private ILogger<Roles> Logger { get; set; }


    private List<RoleListItem> _roles = [];
    private RoleEdit _edit;
    private List<string> _bootstrapMappings = [];
    private string _newMappingValue = "";
    private bool _dirty;
    private bool _busy;
    private string _message;
    private string _messageClass = "alert-info";

    private string RoleClaimType => Security.Value.RoleClaim;

    private IReadOnlyList<UnmappedRoleClaim> UnmappedRoleClaims => UnmappedClaims.Recent();

    protected override async Task OnInitializedAsync() => await LoadListAsync();

    private async Task LoadListAsync()
    {
        await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

        _roles = await db.SecurityRoles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleListItem
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsSystem = role.IsSystem,
                MappingCount = role.IdpMappings.Count,
                PermissionCount = role.Permissions.Count,
                HasWildcard = role.Permissions.Any(p => p.Permission == PermissionRegistry.Wildcard)
            })
            .ToListAsync();
    }

    private void StartCreate()
    {
        _edit = new RoleEdit();
        _bootstrapMappings = [];
        _newMappingValue = "";
        _dirty = true;
        ClearMessage();
    }

    private async Task SelectAsync(int roleId)
    {
        await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();

        SecurityRole role = await db.SecurityRoles
            .AsNoTracking()
            .Include(r => r.IdpMappings)
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role is null)
        {
            await LoadListAsync();
            return;
        }

        _edit = new RoleEdit
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            HasWildcard = role.Permissions.Any(p => p.Permission == PermissionRegistry.Wildcard),
            Mappings = role.IdpMappings
                .OrderBy(mapping => mapping.ClaimValue, StringComparer.OrdinalIgnoreCase)
                .Select(mapping => new RoleMappingEdit
                {
                    Id = mapping.Id,
                    ClaimValue = mapping.ClaimValue,
                    Description = mapping.Description
                })
                .ToList(),
            Permissions = role.Permissions.Select(p => p.Permission).ToHashSet(StringComparer.Ordinal),
            OriginalPermissions = role.Permissions.Select(p => p.Permission).ToHashSet(StringComparer.Ordinal)
        };

        // Bootstrap mappings are unioned in when the snapshot is built, so show them read-only.
        _bootstrapMappings = role.IsSystem
            ? Security.Value.AdminRole.ToList()
            : [];

        _newMappingValue = "";
        _dirty = false;
        ClearMessage();
    }

    private void MarkDirty() => _dirty = true;

    private void ClearUnmappedClaims() => UnmappedClaims.Clear();

    private void AddMapping()
    {
        if (_edit is null)
        {
            return;
        }

        string value = _newMappingValue.Trim();

        if (value.Length == 0)
        {
            return;
        }

        bool alreadyMapped =
            _edit.Mappings.Any(m => m.ClaimValue.Equals(value, StringComparison.OrdinalIgnoreCase)) ||
            _bootstrapMappings.Any(b => b.Equals(value, StringComparison.OrdinalIgnoreCase));

        if (alreadyMapped)
        {
            ShowMessage("That claim value is already mapped to this role.", "alert-warning");
            return;
        }

        _edit.Mappings.Add(new RoleMappingEdit { Id = 0, ClaimValue = value });
        _newMappingValue = "";
        _dirty = true;
        ClearMessage();
    }

    private void RemoveMapping(RoleMappingEdit mapping)
    {
        _edit?.Mappings.Remove(mapping);
        _dirty = true;
    }

    private async Task SaveAsync()
    {
        if (_edit is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_edit.Name))
        {
            ShowMessage("Give the role a name.", "alert-error");
            return;
        }

        // Orphaned grants may be saved back, but an unknown permission must never be newly added.
        string unknownAddition = _edit.Permissions
            .Except(_edit.OriginalPermissions, StringComparer.Ordinal)
            .FirstOrDefault(p => p != PermissionRegistry.Wildcard && !Registry.Contains(p));

        if (unknownAddition is not null)
        {
            ShowMessage($"'{unknownAddition}' is not a permission any loaded plugin declares.", "alert-error");
            return;
        }

        _busy = true;

        try
        {
            await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();
            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

            SecurityRole role;

            if (_edit.Id == 0)
            {
                role = new SecurityRole { Name = _edit.Name.Trim(), Description = _edit.Description };
                db.SecurityRoles.Add(role);
            }
            else
            {
                role = await db.SecurityRoles
                    .Include(r => r.IdpMappings)
                    .Include(r => r.Permissions)
                    .FirstAsync(r => r.Id == _edit.Id);

                if (!role.IsSystem)
                {
                    role.Name = _edit.Name.Trim();
                }

                role.Description = _edit.Description;
            }

            ApplyMappings(db, role);

            if (!role.IsSystem)
            {
                ApplyPermissions(db, role);
            }

            await db.SaveChangesAsync();

            // Checked against the saved state inside the transaction, so a change that would
            // lock the current administrator out is rolled back rather than committed.
            if (await WouldLoseOwnAccessAsync(db, isNewRole: _edit.Id == 0))
            {
                await transaction.RollbackAsync();

                ShowMessage(
                    "That change would remove your own access to this page, so it was not saved. " +
                    "Map another identity provider role first, or use the bootstrap setting in appsettings.",
                    "alert-error");

                return;
            }

            await transaction.CommitAsync();
            await Snapshots.ReloadAsync();

            Logger.LogInformation(
                "Role {RoleId} '{RoleName}' saved by {Subject}.",
                role.Id, role.Name, await CurrentSubjectAsync());

            int savedId = role.Id;

            await LoadListAsync();
            await SelectAsync(savedId);

            ShowMessage("Saved. The change is already in effect.", "alert-success");
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to save role {RoleId}.", _edit.Id);
            ShowMessage("The role could not be saved. A role with that name may already exist.", "alert-error");
        }
        finally
        {
            _busy = false;
        }
    }

    private void ApplyMappings(SegnoSharpDbContext db, SecurityRole role)
    {
        if (_edit is null)
        {
            return;
        }

        HashSet<int> keptIds = _edit.Mappings
            .Where(mapping => mapping.Id != 0)
            .Select(mapping => mapping.Id)
            .ToHashSet();

        foreach (SecurityRoleIdpMapping removed in role.IdpMappings
                     .Where(mapping => !keptIds.Contains(mapping.Id))
                     .ToList())
        {
            role.IdpMappings.Remove(removed);
            db.Remove(removed);
        }

        foreach (RoleMappingEdit mapping in _edit.Mappings)
        {
            if (mapping.Id == 0)
            {
                role.IdpMappings.Add(new SecurityRoleIdpMapping
                {
                    ClaimValue = mapping.ClaimValue,
                    Description = mapping.Description
                });

                continue;
            }

            SecurityRoleIdpMapping existing = role.IdpMappings.FirstOrDefault(m => m.Id == mapping.Id);

            if (existing is not null)
            {
                existing.Description = mapping.Description;
            }
        }
    }

    private void ApplyPermissions(SegnoSharpDbContext db, SecurityRole role)
    {
        if (_edit is null)
        {
            return;
        }

        foreach (SecurityRolePermission removed in role.Permissions
                     .Where(permission => !_edit.Permissions.Contains(permission.Permission))
                     .ToList())
        {
            role.Permissions.Remove(removed);
            db.Remove(removed);
        }

        HashSet<string> current = role.Permissions
            .Select(permission => permission.Permission)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string added in _edit.Permissions.Where(permission => !current.Contains(permission)))
        {
            role.Permissions.Add(new SecurityRolePermission { Permission = added });
        }
    }

    private async Task DeleteAsync()
    {
        if (_edit is null || _edit.Id == 0)
        {
            return;
        }

        var confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Delete the role '{_edit.Name}'? Anyone who reaches it through their identity provider " +
            "role will lose the permissions it grants.");

        if (!confirmed)
        {
            return;
        }

        _busy = true;

        try
        {
            await using SegnoSharpDbContext db = await DbFactory.CreateDbContextAsync();
            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

            SecurityRole role = await db.SecurityRoles.FirstOrDefaultAsync(r => r.Id == _edit.Id);

            if (role is null || role.IsSystem)
            {
                await transaction.RollbackAsync();
                await LoadListAsync();
                return;
            }

            string name = role.Name;
            int id = role.Id;

            db.SecurityRoles.Remove(role);
            await db.SaveChangesAsync();

            if (await WouldLoseOwnAccessAsync(db, isNewRole: false))
            {
                await transaction.RollbackAsync();
                ShowMessage("Deleting that role would remove your own access to this page.", "alert-error");
                return;
            }

            await transaction.CommitAsync();
            await Snapshots.ReloadAsync();

            Logger.LogWarning(
                "Role {RoleId} '{RoleName}' deleted by {Subject}.", id, name, await CurrentSubjectAsync());

            _edit = null;
            await LoadListAsync();

            ShowMessage("Role deleted.", "alert-success");
        }
        finally
        {
            _busy = false;
        }
    }

    private void Cancel()
    {
        _edit = null;
        _bootstrapMappings = [];
        _newMappingValue = "";
        _dirty = false;

        ClearMessage();
    }

    /// <summary>
    /// Answers whether the pending change would remove the signed-in user's own access to this
    /// page. Creating a role can never do that, so the check is skipped in that case.
    /// </summary>
    private async Task<bool> WouldLoseOwnAccessAsync(SegnoSharpDbContext db, bool isNewRole)
    {
        if (isNewRole)
        {
            return false;
        }

        // Internal role names as already resolved by the claims transformation. Using these
        // avoids re-deriving the mapping here and does not depend on the configured claim type.
        HashSet<string> heldRoleNames = await CurrentAppRoleNamesAsync();

        if (heldRoleNames.Count == 0)
        {
            // Indeterminate. The bootstrap setting in appsettings is the recovery path, so warn
            // rather than blocking a legitimate save.
            Logger.LogWarning(
                "No '{RolesClaim}' claims found for the current user; the lockout check was skipped.",
                Constants.RolesClaim);

            return false;
        }

        HashSet<string> heldClaimValues = await CurrentUserRoleClaimsAsync();

        List<SecurityRole> roles = await db.SecurityRoles
            .Include(role => role.IdpMappings)
            .Include(role => role.Permissions)
            .Where(role => heldRoleNames.Contains(role.Name))
            .ToListAsync();

        foreach (SecurityRole role in roles)
        {
            bool grantsAccess = role.Permissions.Any(permission =>
                permission.Permission == PermissionRegistry.Wildcard ||
                permission.Permission == CorePermissions.SecurityEdit);

            if (!grantsAccess)
            {
                continue;
            }

            // Without the raw claim values, assume the role is still reachable: it was a
            // moment ago, and this check is a safety net rather than a security boundary.
            if (heldClaimValues.Count == 0)
            {
                return false;
            }

            bool reachable =
                role.IdpMappings.Any(mapping => heldClaimValues.Contains(mapping.ClaimValue)) ||
                (role.IsSystem && Security.Value.AdminRole.Any(heldClaimValues.Contains));

            if (reachable)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<HashSet<string>> CurrentAppRoleNamesAsync()
    {
        AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        return state.User.FindAll(Constants.RolesClaim)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task<HashSet<string>> CurrentUserRoleClaimsAsync()
    {
        AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        return state.User.FindAll(RoleClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string> CurrentSubjectAsync()
    {
        AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        return state.User.FindFirst("sub")?.Value
               ?? state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? "unknown";
    }

    /// <summary>
    /// Stored values are UTC but may come back from the database with an unspecified kind,
    /// so the kind is set before converting for display.
    /// </summary>
    private static string Format(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime().ToString("g");

    private void ShowMessage(string message, string cssClass)
    {
        _message = message;
        _messageClass = cssClass;
    }

    private void ClearMessage() => _message = null;
}
