using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Services;

public sealed class UnmappedRoleClaimTracker(ISystemClock clock)
{
    private const int Capacity = 100;

    private readonly ConcurrentDictionary<string, UnmappedRoleClaim> _seen = new(StringComparer.OrdinalIgnoreCase);

    public void Record(string claimValue, string label = null)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return;
        }

        // Bounded so a token carrying many unmapped values cannot grow this without limit.
        if (_seen.Count >= Capacity && !_seen.ContainsKey(claimValue))
        {
            string oldest = _seen
                .OrderBy(entry => entry.Value.LastSeen)
                .Select(entry => entry.Key)
                .FirstOrDefault();

            if (oldest is not null)
            {
                _seen.TryRemove(oldest, out _);
            }
        }

        _seen[claimValue] = new UnmappedRoleClaim(claimValue, label, clock.UtcNow);
    }

    public IReadOnlyList<UnmappedRoleClaim> Recent()
    {
        return _seen.Values
            .OrderByDescending(claim => claim.LastSeen)
            .ToList();
    }

    public void Clear()
    {
        _seen.Clear();
    }
}