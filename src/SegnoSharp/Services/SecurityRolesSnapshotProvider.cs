using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Configuration.Models;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Models.Security;

namespace Whitestone.SegnoSharp.Services;

public sealed class SecurityRolesSnapshotProvider(
    IServiceScopeFactory scopes,
    IOptionsMonitor<SegnoSharpOpenIdConnectOptions> options,
    ILogger<SecurityRolesSnapshotProvider> log)
{
    public SecurityRolesSnapshot Current => _current;

    private volatile SecurityRolesSnapshot _current = SecurityRolesSnapshot.Empty;

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        using IServiceScope scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SegnoSharpDbContext>();

        var rows = await db.SecurityRoles
            .Select(r => new
            {
                r.Id,
                r.Name,
                Permissions = r.Permissions
                    .Select(p => p.Permission)
                    .ToList(),
                Mappings = r.IdpMappings
                    .Select(m => m.ClaimValue)
                    .ToList()
            })
            .ToListAsync(ct);

        Dictionary<int, RoleDefinition> roles = rows.ToDictionary(
            r => r.Id,
            r => new RoleDefinition(
                r.Id,
                r.Name,
                r.Permissions.ToImmutableHashSet(StringComparer.Ordinal)));

        Dictionary<string, List<int>> map = new(StringComparer.OrdinalIgnoreCase);

        foreach (var r in rows)
        {
            foreach (string claimValue in r.Mappings)
            {
                Add(map, claimValue, r.Id);
            }
        }

        if (roles.ContainsKey(SecurityRole.AdministratorRoleId))
        {
            foreach (string claimValue in options.CurrentValue.AdminRole)
            {
                Add(map, claimValue, SecurityRole.AdministratorRoleId);
            }
        }
        else
        {
            log.LogError("System role 'Administrator' is missing; bootstrap mapping not applied.");
        }

        _current = new SecurityRolesSnapshot()
        {
            ClaimToRoles = map.ToFrozenDictionary(
                kv => kv.Key,
                kv => kv.Value.ToImmutableArray(),
                StringComparer.OrdinalIgnoreCase),
            Roles = roles.ToFrozenDictionary()
        };

        log.LogDebug("Access snapshot reloaded: {Roles} roles, {Mappings} claim mappings.", roles.Count, map.Count);
    }

    private static void Add(Dictionary<string, List<int>> m, string key, int roleId)
    {
        if (!m.TryGetValue(key, out List<int> list))
        {
            m[key] = list = [];
        }

        if (!list.Contains(roleId))
        {
            list.Add(roleId);
        }
    }
}