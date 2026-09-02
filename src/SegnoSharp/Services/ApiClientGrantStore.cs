using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;

namespace Whitestone.SegnoSharp.Services;

public class ApiClientGrantStore(SegnoSharpDbContext db, ApiClientGrantCache cache)
{
    public static string CacheKey(int clientId) => $"clientgrants:{clientId}";

    public async Task<ImmutableHashSet<string>> GetPermissionsAsync(int clientId, CancellationToken ct = default)
    {
        if (cache.TryGet(clientId, out ImmutableHashSet<string> cached))
        {
            return cached!;
        }

        List<string> permissions = await db.SecurityApiClientPermissions
            .Where(p => p.SecurityApiClientId == clientId)
            .Select(p => p.Permission)
            .ToListAsync(ct);

        ImmutableHashSet<string> result = permissions.ToImmutableHashSet(StringComparer.Ordinal);

        cache.Set(clientId, result);

        return result;
    }

    public void Evict(int clientId)
    {
        cache.Evict(clientId);
    }
}