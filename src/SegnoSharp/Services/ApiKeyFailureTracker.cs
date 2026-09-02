using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;

namespace Whitestone.SegnoSharp.Services;

public sealed class ApiKeyFailureTracker
{
    private sealed class Counter { public int Value; }

    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 50_000 });

    private const int Threshold = 20;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Penalty = TimeSpan.FromMinutes(5);

    public void RecordFailure(IPAddress ip)
    {
        string key = Partition(ip);
        
        if (key is null)
        {
            return;
        }

        Counter counter = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Window;
            entry.Size = 1;
            
            return new Counter();
        })!;

        if (Interlocked.Increment(ref counter.Value) == Threshold)
        {
            _cache.Set("block:" + key, true, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = Penalty,
                Size = 1
            });
        }
    }

    public bool IsBlocked(IPAddress ip)
    {
        string key = Partition(ip);
        
        return key is not null && _cache.TryGetValue("block:" + key, out _);
    }

    // IPv6 partitioned by /64 — one host typically holds many addresses in one.
    private static string Partition(IPAddress ip)
    {
        if (ip is null)
        {
            return null;
        }

        if (ip.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return ip.ToString();
        }

        byte[] bytes = ip.GetAddressBytes();

        return Convert.ToHexString(bytes.AsSpan(0, 8));
    }
}