using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;

namespace Whitestone.SegnoSharp.Services;

public sealed class ApiKeyUsageFlusher(
    IServiceScopeFactory scopes,
    ApiKeyUsageBuffer buffer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(ct))
        {
            IReadOnlyList<KeyValuePair<int, DateTime>> batch = buffer.Drain();
            
            if (batch.Count == 0)
            {
                continue;
            }

            using IServiceScope scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SegnoSharpDbContext>();

            foreach ((int keyId, DateTime at) in batch)
            {
                await db.SecurityApiKeys
                    .Where(k => k.Id == keyId)
                    .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsed, at), ct);
            }
        }
    }
}