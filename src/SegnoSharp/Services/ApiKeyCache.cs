using System;
using Microsoft.Extensions.Caching.Memory;
using Whitestone.SegnoSharp.Models.Security;

namespace Whitestone.SegnoSharp.Services
{
    public sealed class ApiKeyCache : IDisposable
    {
        // Size-limited: random well-formed prefixes must not grow this unbounded.
        private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 20_000 });

        private static readonly MemoryCacheEntryOptions HitOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30), Size = 1
        };

        private static readonly MemoryCacheEntryOptions MissOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10), Size = 1
        };

        public bool TryGet(string prefix, out ApiKeyRecord record)
        {
            return _cache.TryGetValue(prefix, out record);
        }

        public void Set(string prefix, ApiKeyRecord record)
        {
            _cache.Set(prefix, record, record is null ? MissOptions : HitOptions);
        }

        public void Evict(string prefix)
        {
            _cache.Remove(prefix);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
