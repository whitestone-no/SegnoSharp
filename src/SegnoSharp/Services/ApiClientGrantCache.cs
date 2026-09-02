using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Immutable;

namespace Whitestone.SegnoSharp.Services;

/// <summary>
/// Owns its cache rather than sharing IDistributedMemoryCache so the size limit is enforced
/// independently of anything else in the application.
/// </summary>
public sealed class ApiClientGrantCache : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 5_000 });

    private static readonly MemoryCacheEntryOptions EntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
        Size = 1
    };

    public bool TryGet(int clientId, out ImmutableHashSet<string> permissions)
    {
        return _cache.TryGetValue(clientId, out permissions);
    }

    public void Set(int clientId, ImmutableHashSet<string> permissions)
    {
        _cache.Set(clientId, permissions, EntryOptions);
    }

    public void Evict(int clientId)
    {
        _cache.Remove(clientId);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}