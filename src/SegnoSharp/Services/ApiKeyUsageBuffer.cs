using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Services;

public sealed class ApiKeyUsageBuffer
{
    private readonly ConcurrentDictionary<int, DateTime> _pending = new();

    public void Touch(int keyId, DateTime at) => _pending[keyId] = at;

    public IReadOnlyList<KeyValuePair<int, DateTime>> Drain()
    {
        KeyValuePair<int, DateTime>[] snapshot = _pending.ToArray();
        
        foreach (KeyValuePair<int, DateTime> entry in snapshot)
        {
            _pending.TryRemove(entry);
        }

        return snapshot;
    }
}